window.reelsObserver = {
    observer: null,
    mutationObserver: null,
    dotNetRef: null,
    players: {},
    playPromises: {}, 
    globalMuted: true,
    currentlyActiveVideoId: null,
    staggerTimeout: null,

    initialize: function (dotNetHelper, containerId) {
        this.dotNetRef = dotNetHelper;

        // Automatically focus/scroll the page to center the video feed in the viewport
        const container = document.getElementById(containerId);
        if (container) {
            setTimeout(() => {
                container.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }, 200);

            // MutationObserver to intercept unmounting video elements and force release decoders
            this.mutationObserver = new MutationObserver((mutations) => {
                mutations.forEach(mutation => {
                    mutation.removedNodes.forEach(node => {
                        if (node.nodeName === 'VIDEO') {
                            if (!document.body.contains(node)) {
                                window.reelsObserver.cleanupVideo(node);
                            }
                        } else if (node.querySelectorAll) {
                            node.querySelectorAll('video').forEach(v => {
                                if (!document.body.contains(v)) {
                                    window.reelsObserver.cleanupVideo(v);
                                }
                            });
                        }
                    });
                });
            });
            this.mutationObserver.observe(container, { childList: true, subtree: true });
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
                    window.reelsObserver.currentlyActiveVideoId = videoId;
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

    cleanupVideo: function(videoEl) {
        if (!videoEl) return;
        try {
            videoEl.pause();
            videoEl.removeAttribute('src');
            const sources = videoEl.querySelectorAll('source');
            sources.forEach(s => s.remove());
            videoEl.load();
            console.log("[ReelsObserver] Successfully reclaimed hardware decoder for unmounted video.");
        } catch (e) {
            console.error("[ReelsObserver] Error cleaning up unmounted video:", e);
        }
    },

    observeVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        const videoElement = document.getElementById(videoId);

        if (videoElement) {
            const existingPlayer = this.players[videoId];
            if (existingPlayer && existingPlayer.media !== videoElement) {
                try {
                    existingPlayer.destroy();
                } catch(e) {}
                delete this.players[videoId];
            }
        }

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

            // If this video is the currently active/intersecting one, play it immediately!
            if (videoId === window.reelsObserver.currentlyActiveVideoId) {
                const activePlayer = this.players[videoId];
                activePlayer.__ignoreVolumeChangeUntil = Date.now() + 200;
                activePlayer.muted = window.reelsObserver.globalMuted;
                if (!window.reelsObserver.globalMuted) {
                    activePlayer.volume = 1;
                }
                activePlayer.play().catch(e => {
                    console.log('Autoplay prevented on lazy load init', e);
                });
            }
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
                document.body.classList.remove('bs-body-theater-mode');
            });

            let lastClickTime = 0;
            let clickTimeout = null;
            element.addEventListener('click', (e) => {
                if (e.target.closest('.bs-reel-action-btn') || 
                    e.target.closest('.plyr__controls') || 
                    e.target.closest('.plyr__control--overlaid') || 
                    e.target.closest('.bs-theater-btn')) return;

                const container = element.closest('.bs-reels-container') || element.closest('.reels-feed-container');
                const isReelsPage = !!element.closest('.reels-page-container');
                const isInTheaterMode = isReelsPage || element.classList.contains('bs-theater-mode') || 
                                         (container && container.classList.contains('in-theater-mode'));

                if (isInTheaterMode) {
                    const currentTime = new Date().getTime();
                    const timeDiff = currentTime - lastClickTime;
                    
                    if (timeDiff < 300) {
                        // Double tap/click detected: Exit theater mode
                        if (clickTimeout) {
                            clearTimeout(clickTimeout);
                            clickTimeout = null;
                        }
                        
                        element.classList.remove('bs-theater-mode');
                        if (container) {
                            container.classList.remove('in-theater-mode');
                        }
                        document.body.classList.remove('bs-body-theater-mode');
                        
                        // Ensure the video continues playing when exiting
                        const player = window.reelsObserver.players[videoId];
                        if (player) {
                            player.play().catch(e => {});
                        } else {
                            const nativeVideo = element.querySelector('video');
                            if (nativeVideo) {
                                nativeVideo.play().catch(e => {});
                            }
                        }
                        return;
                    }
                    lastClickTime = currentTime;

                    if (clickTimeout) {
                        clearTimeout(clickTimeout);
                    }
                    
                    clickTimeout = setTimeout(() => {
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
                    }, 250);
                } else {
                    // Enter theater mode on single click
                    element.classList.add('bs-theater-mode');
                    if (container) {
                        container.classList.add('in-theater-mode');
                    }
                    document.body.classList.add('bs-body-theater-mode');
                    
                    // Auto-play the video when entering theater mode
                    const player = window.reelsObserver.players[videoId];
                    if (player) {
                        player.play().catch(e => {});
                    } else {
                        // Native video fallback (e.g. in ReelsFeed.razor)
                        const nativeVideo = element.querySelector('video');
                        if (nativeVideo) {
                            nativeVideo.play().catch(e => {});
                        }
                    }
                }
            });
        }
    },

    unobserveVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        if (element) {
            // Unload the video element to force release browser hardware decoders
            const videoEl = element.querySelector('video');
            if (videoEl) {
                window.reelsObserver.cleanupVideo(videoEl);
            }

            if (this.observer) {
                this.observer.unobserve(element);
            }
        }
        if (this.players[videoId]) {
            try {
                this.players[videoId].destroy();
            } catch (e) {}
            delete this.players[videoId];
        }
    },

    dispose: function () {
        if (this.mutationObserver) {
            this.mutationObserver.disconnect();
            this.mutationObserver = null;
        }
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.clearAll();
        this.dotNetRef = null;
    },

    clearAll: function () {
        this.currentlyActiveVideoId = null;
        if (this.staggerTimeout) {
            clearTimeout(this.staggerTimeout);
            this.staggerTimeout = null;
        }
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
        
        const container = document.getElementById("reels-feed-container");
        if (container) {
            // Unload all native video tags inside the container
            container.querySelectorAll('video').forEach(videoEl => {
                window.reelsObserver.cleanupVideo(videoEl);
            });
            container.scrollTop = 0;
        }
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
                        if (originalTime === 0) {
                            videoEl.currentTime = 0.001; 
                        } else {
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
            
            // Stagger preloading the next video to avoid network contention with active video.
            // We set next video to preload="metadata" initially, then upgrade to "auto" after 1.5s.
            if (next) {
                const nextVideoId = next.getAttribute('data-video-id');
                if (nextVideoId) {
                    const nextVideoEl = document.getElementById(nextVideoId);
                    if (nextVideoEl) {
                        nextVideoEl.setAttribute('preload', 'metadata');
                        
                        if (this.staggerTimeout) {
                            clearTimeout(this.staggerTimeout);
                        }
                        
                        this.staggerTimeout = setTimeout(() => {
                            // Upgrade to auto and trigger load
                            nextVideoEl.setAttribute('preload', 'auto');
                            try {
                                nextVideoEl.load();
                                // After metadata/media starts loading, try pre-decoding its frame
                                setTimeout(() => {
                                    tryPreDecode(next);
                                }, 300);
                            } catch(e) {}
                        }, 1500);
                    }
                }
            }
            
        } catch (e) {
            console.error("Failed to pre-decode adjacent videos", e);
        }
    },

    pauseVideo: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            this.safePause(videoId, player);
        }
    },

    closeTheaterMode: function () {
        const container = document.querySelector('.bs-reels-container.in-theater-mode');
        if (container) {
            container.classList.remove('in-theater-mode');
            const activeItem = container.querySelector('.bs-reel-item.bs-theater-mode');
            if (activeItem) {
                activeItem.classList.remove('bs-theater-mode');
            }
            document.body.classList.remove('bs-body-theater-mode');
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
        document.body.classList.remove('bs-body-theater-mode');
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
    } else if (e.key === 'ArrowRight') {
        e.preventDefault();
        const videoId = activeItem.getAttribute('data-video-id');
        if (videoId) {
            const player = window.reelsObserver.players[videoId];
            if (player) {
                player.__ignoreVolumeChangeUntil = Date.now() + 200;
                player.volume = Math.min(1.0, player.volume + 0.1);
                if (player.muted) {
                    player.muted = false;
                    window.reelsObserver.globalMuted = false;
                }
            }
        }
    } else if (e.key === 'ArrowLeft') {
        e.preventDefault();
        const videoId = activeItem.getAttribute('data-video-id');
        if (videoId) {
            const player = window.reelsObserver.players[videoId];
            if (player) {
                player.__ignoreVolumeChangeUntil = Date.now() + 200;
                player.volume = Math.max(0.0, player.volume - 0.1);
                if (player.volume === 0) {
                    player.muted = true;
                    window.reelsObserver.globalMuted = true;
                }
            }
        }
    } else if (e.key === 'AudioVolumeUp') {
        const videoId = activeItem.getAttribute('data-video-id');
        if (videoId) {
            const player = window.reelsObserver.players[videoId];
            if (player && player.muted) {
                player.__ignoreVolumeChangeUntil = Date.now() + 200;
                player.muted = false;
                window.reelsObserver.globalMuted = false;
            }
        }
    }
});

window.showBuildSmartToast = function (message, type, actionUrl) {
    let container = document.getElementById('bs-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'bs-toast-container';
        container.className = 'bs-toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `bs-toast bs-toast-${type || 'info'} ${actionUrl ? 'clickable' : ''}`;

    if (actionUrl) {
        toast.addEventListener('click', (e) => {
            if (e.target.closest('.bs-toast-close')) return;
            window.location.href = actionUrl;
        });
    }

    let icon = '';
    if (type === 'success') {
        icon = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-success)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>`;
    } else if (type === 'error' || type === 'danger') {
        icon = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-danger)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>`;
    } else {
        icon = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-info)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>`;
    }

    toast.innerHTML = `
        <div class="bs-toast-icon">${icon}</div>
        <div class="bs-toast-content">${message}</div>
        <button class="bs-toast-close" onclick="this.parentElement.remove()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
        </button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('bs-toast-fadeout');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, 4000);
};