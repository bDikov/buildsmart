window.reelsObserver = {
    observer: null,
    dotNetRef: null,
    players: {},
    playPromises: {}, 
    globalMuted: true,

    initialize: function (dotNetHelper, containerId) {
        this.dotNetRef = dotNetHelper;

        // Automatically focus/scroll the page to center the video feed in the viewport
        const container = document.getElementById(containerId);
        if (container) {
            setTimeout(() => {
                container.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }, 200);
        }

        let options = {
            root: document.getElementById(containerId),
            rootMargin: '0px',
            threshold: 0.6 
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const videoId = entry.target.getAttribute('data-video-id');
                const player = this.players[videoId];

                if (entry.isIntersecting) {
                    if (player) {
                        player.__ignoreVolumeChangeUntil = Date.now() + 200;
                        player.muted = window.reelsObserver.globalMuted;
                        if (!window.reelsObserver.globalMuted) {
                            player.volume = 1;
                        }
                        
                        for (const id in window.reelsObserver.players) {
                            if (id !== videoId) {
                                const p = window.reelsObserver.players[id];
                                if (p) {
                                    p.__ignoreVolumeChangeUntil = Date.now() + 200;
                                    p.muted = true;
                                    window.reelsObserver.safePause(id, p);
                                }
                            }
                        }

                        let playPromise = player.play();
                        if (playPromise !== undefined) {
                            window.reelsObserver.playPromises[videoId] = playPromise;
                            playPromise.catch(e => {
                                if (e.name !== 'AbortError' && !player.muted) {
                                    player.__ignoreVolumeChangeUntil = Date.now() + 200;
                                    player.muted = true;
                                    
                                    let fallbackPromise = player.play();
                                    if (fallbackPromise !== undefined) {
                                        window.reelsObserver.playPromises[videoId] = fallbackPromise;
                                    }
                                }
                                console.log('Autoplay prevented by browser, falling back to muted', e);
                            });
                        }
                    }
                    this.dotNetRef.invokeMethodAsync('OnVideoVisible', videoId);
                    window.reelsObserver.preDecodeAdjacent(entry.target);

                    const container = entry.target.closest('.bs-reels-container');
                    if (container && container.classList.contains('in-theater-mode')) {
                        container.querySelectorAll('.bs-reel-item').forEach(item => {
                            item.classList.remove('bs-theater-mode');
                        });
                        entry.target.classList.add('bs-theater-mode');
                    }
                } else {
                    if (player) {
                        player.__ignoreVolumeChangeUntil = Date.now() + 200;
                        player.muted = true; 
                        window.reelsObserver.safePause(videoId, player);
                    }
                }
            });
        }, options);
    },

    safePause: function(videoId, player) {
        const playPromise = this.playPromises[videoId];
        if (playPromise !== undefined) {
            playPromise.then(_ => {
                player.pause();
            }).catch(e => {});
        } else {
            player.pause();
        }
        delete this.playPromises[videoId];
    },

    observeVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        const videoElement = document.getElementById(videoId);

        if (videoElement && !this.players[videoId]) {
            this.players[videoId] = new Plyr(videoElement, {
                controls: ['play-large', 'play', 'progress', 'current-time', 'mute', 'volume'],
                autoplay: false,
                muted: window.reelsObserver.globalMuted, 
                clickToPlay: false,
                fullscreen: { enabled: false, fallback: false },
                doubleClick: { toggles: false } 
            });
            
            this.players[videoId].__ignoreVolumeChangeUntil = Date.now() + 300;
            
            this.players[videoId].on('volumechange', (e) => {
                const p = e.detail.plyr;
                if (p.__ignoreVolumeChangeUntil && Date.now() < p.__ignoreVolumeChangeUntil) return;
                
                if (!p.muted && p.volume > 0) {
                    window.reelsObserver.globalMuted = false;
                } else {
                    window.reelsObserver.globalMuted = true;
                }
            });
        }

        if (element && this.observer) {
            this.observer.observe(element);
        }

        if (element && !element.__swipeInitialized) {
            element.__swipeInitialized = true;
            let tapCount = 0;
            let tapTimer = null;

            // Wire up the close button to aggressively block Plyr from capturing the touch
            const closeBtn = element.querySelector('.bs-theater-close');

            const handleTheaterBtn = (e, action) => {
                e.preventDefault();
                e.stopPropagation();
                if (e.type === 'touchstart' || e.type === 'mousedown') {
                    action();
                }
            };

            const bindButton = (btn, action) => {
                if (!btn) return;
                btn.addEventListener('touchstart', (e) => handleTheaterBtn(e, action), { passive: false });
                btn.addEventListener('mousedown', (e) => handleTheaterBtn(e, action));
                btn.addEventListener('click', (e) => { e.preventDefault(); e.stopPropagation(); });
            };

            bindButton(closeBtn, () => {
                element.classList.remove('bs-theater-mode');
                const container = element.closest('.bs-reels-container');
                if (container) container.classList.remove('in-theater-mode');
            });

            element.addEventListener('click', (e) => {
                if (e.target.closest('.bs-reel-action-btn') || 
                    e.target.closest('.plyr__controls') || 
                    e.target.closest('.plyr__control--overlaid') || 
                    e.target.closest('.bs-theater-btn')) return;

                const container = element.closest('.bs-reels-container') || element.closest('.reels-feed-container');
                const isReelsPage = !!element.closest('.reels-page-container');
                const isInTheaterMode = isReelsPage || element.classList.contains('bs-theater-mode') || 
                                        (container && container.classList.contains('in-theater-mode'));

                if (!isInTheaterMode) {
                    // Enter theater mode on single click
                    element.classList.add('bs-theater-mode');
                    if (container) {
                        container.classList.add('in-theater-mode');
                    }
                    
                    // Auto-play the video when entering theater mode
                    const player = window.reelsObserver.players[videoId];
                    if (player) {
                        if (player.paused) {
                            player.play().catch(e => {});
                        }
                    } else {
                        // Native video fallback (e.g. in ReelsFeed.razor)
                        const nativeVideo = element.querySelector('video');
                        if (nativeVideo && nativeVideo.paused) {
                            nativeVideo.play().catch(e => {});
                        }
                    }
                } else {
                    // Toggle play/pause when already in theater mode
                    const player = window.reelsObserver.players[videoId];
                    if (player) {
                        player.togglePlay();
                        
                        const plyrContainer = document.getElementById(videoId)?.closest('.plyr');
                        if (plyrContainer) {
                            const areControlsHidden = plyrContainer.classList.contains('plyr--hide-controls');
                            if (areControlsHidden) {
                                player.toggleControls(true);
                            } else {
                                player.toggleControls(false);
                            }
                        }
                    } else {
                        // Native video fallback (e.g. in ReelsFeed.razor)
                        const nativeVideo = element.querySelector('video');
                        if (nativeVideo) {
                            if (nativeVideo.paused) {
                                nativeVideo.play().catch(e => {});
                            } else {
                                nativeVideo.pause();
                            }
                        }
                    }
                }
            });
        }
    },

    unobserveVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        if (element && this.observer) {
            this.observer.unobserve(element);
        }
        if (this.players[videoId]) {
            this.players[videoId].destroy();
            delete this.players[videoId];
        }
    },

    dispose: function () {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.clearAll();
        this.dotNetRef = null;
    },

    clearAll: function () {
        for (const videoId in this.players) {
            if (this.players[videoId]) {
                try {
                    this.players[videoId].pause();
                    this.players[videoId].destroy();
                } catch (e) { }
            }
        }
        this.players = {};
        this.playPromises = {};
    },

    togglePlayback: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            player.togglePlay();
            if (player.muted) {
                player.__ignoreVolumeChangeUntil = Date.now() + 200;
                player.muted = false; 
                window.reelsObserver.globalMuted = false;
            }
        }
    },

    playVideo: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            player.__ignoreVolumeChangeUntil = Date.now() + 200;
            player.muted = window.reelsObserver.globalMuted;
            if (!window.reelsObserver.globalMuted) {
                player.volume = 1;
            }
            
            let playPromise = player.play();
            if (playPromise !== undefined) {
                playPromise.catch(e => {
                    if (!player.muted) {
                        player.__ignoreVolumeChangeUntil = Date.now() + 200;
                        player.muted = true;
                        player.play();
                    }
                    console.log('Autoplay prevented by browser, falling back to muted', e);
                });
            }
        }
    },

    preDecodeAdjacent: function(centerElement) {
        if (!centerElement) return;
        
        try {
            const tryPreDecode = (element) => {
                if (!element) return;
                const videoId = element.getAttribute('data-video-id');
                if (videoId) {
                    const videoEl = document.getElementById(videoId);
                    if (videoEl && videoEl.readyState >= 1) { // HAVE_METADATA or better
                        // Silently nudge the time to force a frame decode, then instantly put it back
                        const originalTime = videoEl.currentTime;
                        
                        // Only nudge if the video is currently at the very beginning (0)
                        // If the user already watched some of it, the frame is already cached!
                        if (originalTime === 0) {
                            videoEl.currentTime = 0.001; 
                        } else {
                            // If they already watched it, nudge it forward slightly and back to force a refresh
                            // without losing their spot.
                            videoEl.currentTime = originalTime + 0.001;
                            setTimeout(() => {
                                if (videoEl.paused) {
                                    videoEl.currentTime = originalTime;
                                }
                            }, 50);
                        }
                    }
                }
            };

            // Previous video (up)
            let prev = centerElement.previousElementSibling;
            tryPreDecode(prev);

            // Next video (down)
            let next = centerElement.nextElementSibling;
            tryPreDecode(next);
            
        } catch (e) {
            console.error("Failed to pre-decode adjacent videos", e);
        }
    },

    pauseVideo: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            this.safePause(videoId, player);
        }
    }
};

window.addEventListener('keydown', (e) => {
    const container = document.querySelector('.bs-reels-container.in-theater-mode');
    if (!container) return;

    const activeItem = container.querySelector('.bs-reel-item.bs-theater-mode');
    if (!activeItem) return;

    if (e.key === 'ArrowDown') {
        e.preventDefault();
        container.scrollBy({ top: container.clientHeight, behavior: 'smooth' });
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        container.scrollBy({ top: -container.clientHeight, behavior: 'smooth' });
    } else if (e.key === 'Escape') {
        e.preventDefault();
        activeItem.classList.remove('bs-theater-mode');
        container.classList.remove('in-theater-mode');
    } else if (e.key === ' ') {
        e.preventDefault();
        const videoId = activeItem.getAttribute('data-video-id');
        if (videoId) {
            window.reelsObserver.togglePlayback(videoId);
        }
    } else if (e.key.toLowerCase() === 'm') {
        e.preventDefault();
        const videoId = activeItem.getAttribute('data-video-id');
        if (videoId) {
            const player = window.reelsObserver.players[videoId];
            if (player) {
                player.muted = !player.muted;
                window.reelsObserver.globalMuted = player.muted;
            }
        }
    }
});