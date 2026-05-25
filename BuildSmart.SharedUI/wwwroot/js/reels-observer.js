window.reelsObserver = {
    observer: null,
    dotNetRef: null,
    players: {},
    playPromises: {}, 
    globalMuted: true,

    initialize: function (dotNetHelper, containerId) {
        this.dotNetRef = dotNetHelper;

        let options = {
            root: document.getElementById(containerId),
            rootMargin: '0px',
            threshold: 0.6 
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const videoId = entry.target.getAttribute('data-video-id');
                const player = this.players[videoId];

                const isTopCard = !entry.target.nextElementSibling;
                const isTheaterMode = entry.target.classList.contains('bs-theater-mode');

                if (entry.isIntersecting && (isTopCard || isTheaterMode)) {
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
                } else {
                    if (player) {
                        if (!isTheaterMode) {
                            player.__ignoreVolumeChangeUntil = Date.now() + 200;
                            player.muted = true; 
                            window.reelsObserver.safePause(videoId, player);
                        }
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
                player.currentTime = 0;
            }).catch(e => {});
        } else {
            player.pause();
            player.currentTime = 0;
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
            let touchStartX = null;
            let touchStartY = null;
            let isSwiping = false;
            let tapCount = 0;
            let tapTimer = null;

            element.addEventListener('click', (e) => {
                if (e.target.closest('.bs-reel-action-btn') || e.target.closest('.plyr__controls')) return;

                tapCount++;
                if (tapCount === 1) {
                    tapTimer = setTimeout(() => {
                        const player = window.reelsObserver.players[videoId];
                        if (player) player.togglePlay();
                        tapCount = 0;
                    }, 300);
                } else if (tapCount === 2) {
                    element.classList.toggle('bs-theater-mode');
                    tapCount = 0;
                    clearTimeout(tapTimer);
                }
            });

            const startSwipe = (x, y) => {
                touchStartX = x;
                touchStartY = y;
                isSwiping = true;
                element.style.transition = 'none';
            };

            const moveSwipe = (x, y) => {
                if (!isSwiping || touchStartX === null) return;
                const deltaX = x - touchStartX;
                const deltaY = y - touchStartY;
                const currentX = deltaX * 0.85;
                const currentY = deltaY * 0.85;
                const currentRot = currentX * 0.05;
                element.style.transform = `translate(calc(-50% + ${currentX}px), calc(-50% + ${currentY}px)) rotate(${currentRot}deg)`;
            };

            const endSwipe = (x, y) => {
                if (!isSwiping || touchStartX === null) return;
                isSwiping = false;
                const deltaX = x - touchStartX;
                const deltaY = y - touchStartY;
                touchStartX = null;

                if (Math.abs(deltaX) > 80 || Math.abs(deltaY) > 80) {
                    const flyX = deltaX > 0 ? 1000 : -1000;
                    const rotate = deltaX > 0 ? 25 : -25;
                    
                    element.style.transition = 'transform 0.45s cubic-bezier(0.1, 0.7, 0.1, 1)';
                    element.style.transform = `translate(calc(-50% + ${flyX}px), calc(-50% - 800px)) rotate(${rotate}deg)`;

                    // SYNCHRONOUS TRUSTED PLAY: Bypass browser autoplay block by playing the next video right here inside the touch event!
                    let nextVideoId = null;
                    if (deltaX < 0) {
                        // Swiping left (Next Video) -> next video is the one right underneath in the DOM
                        const nextElement = element.previousElementSibling;
                        if (nextElement) nextVideoId = nextElement.getAttribute('data-video-id');
                    } else {
                        // Swiping right (Previous Video) -> next video is the absolute bottom card of the DOM
                        const bottomElement = element.parentElement.firstElementChild;
                        if (bottomElement) nextVideoId = bottomElement.getAttribute('data-video-id');
                    }
                    
                    if (nextVideoId) {
                        window.reelsObserver.playVideo(nextVideoId);
                    }

                    setTimeout(() => {
                        if (window.reelsObserver.dotNetRef) {
                            window.reelsObserver.dotNetRef.invokeMethodAsync('ProcessSwipeEndFromJS', deltaX, 0, 0, 0, videoId);
                        }
                        element.style.transition = 'none';
                        element.style.transform = '';
                    }, 400);
                } else {
                    element.style.transition = 'transform 0.5s cubic-bezier(0.2, 1.2, 0.3, 1)';
                    element.style.transform = '';
                }
            };

            element.addEventListener('touchstart', (e) => startSwipe(e.touches[0].clientX, e.touches[0].clientY), { passive: true });
            element.addEventListener('touchmove', (e) => {
                if (isSwiping && e.cancelable) e.preventDefault();
                moveSwipe(e.touches[0].clientX, e.touches[0].clientY);
            }, { passive: false });
            element.addEventListener('touchend', (e) => {
                if (e.changedTouches.length > 0) endSwipe(e.changedTouches[0].clientX, e.changedTouches[0].clientY);
            });

            element.addEventListener('mousedown', (e) => startSwipe(e.clientX, e.clientY));
            window.addEventListener('mousemove', (e) => { if (isSwiping) moveSwipe(e.clientX, e.clientY); });
            window.addEventListener('mouseup', (e) => { if (isSwiping) endSwipe(e.clientX, e.clientY); });
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

    pauseVideo: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            this.safePause(videoId, player);
        }
    }
};