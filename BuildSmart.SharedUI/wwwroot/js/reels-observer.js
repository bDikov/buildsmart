window.reelsObserver = {
    observer: null,
    dotNetRef: null,
    players: {},
    playPromises: {}, // Track pending play promises to prevent AbortErrors
    globalMuted: true, // Track user's mute intent across all videos

    initialize: function (dotNetHelper, containerId) {
        this.dotNetRef = dotNetHelper;

        let options = {
            root: document.getElementById(containerId),
            rootMargin: '0px',
            threshold: 0.6 // Trigger when 60% of the video is visible
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const videoId = entry.target.getAttribute('data-video-id');
                const player = this.players[videoId];

                if (entry.isIntersecting) {
                    if (player) {
                        // Apply the global mute state chosen by the user
                        player.muted = window.reelsObserver.globalMuted;
                        if (!window.reelsObserver.globalMuted) {
                            player.volume = 1;
                        }
                        
                        let playPromise = player.play();
                        if (playPromise !== undefined) {
                            window.reelsObserver.playPromises[videoId] = playPromise;
                            playPromise.catch(e => {
                                // If the browser blocks unmuted autoplay, fallback to muted so the video still plays
                                if (e.name !== 'AbortError' && !player.muted) {
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
                    // Notify Blazor that this video is now active
                    this.dotNetRef.invokeMethodAsync('OnVideoVisible', videoId);
                } else {
                    if (player) {
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
                player.currentTime = 0; // Rewind to start
            }).catch(e => {
                // Play was already aborted or failed, safe to ignore
            });
        } else {
            player.pause();
            player.currentTime = 0;
        }
        delete this.playPromises[videoId];
    },

    observeVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        const videoElement = document.getElementById(videoId);

        // Initialize Plyr if not already initialized for this video
        if (videoElement && !this.players[videoId]) {
            this.players[videoId] = new Plyr(videoElement, {
                controls: ['play-large', 'play', 'progress', 'current-time', 'mute', 'volume'],
                autoplay: false,
                muted: true,
                clickToPlay: false, // Disable single click to play/pause so we can use double-click
                fullscreen: { enabled: false, fallback: false }, // Completely disable fullscreen to prevent double-click hijacking
                doubleClick: { toggles: false } // Prevent Plyr from listening to double clicks natively
            });
            
            // Listen for user volume/mute changes to sync across all videos
            this.players[videoId].on('volumechange', (e) => {
                const p = e.detail.plyr;
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

        // --- ADD TINDER-STYLE SWIPE LOGIC TO THE REEL WRAPPER ---
        if (element && !element.__swipeInitialized) {
            element.__swipeInitialized = true;
            let touchStartX = null;
            let touchStartY = null;
            let isSwiping = false;

            // Custom Tap Logic for Single Click (Play/Pause) and Double Click (Theater Mode)
            let tapCount = 0;
            let tapTimer = null;

            element.addEventListener('click', (e) => {
                // Ignore clicks on actual buttons or the plyr controls
                if (e.target.closest('.bs-reel-action-btn') || e.target.closest('.plyr__controls')) return;

                tapCount++;

                if (tapCount === 1) {
                    tapTimer = setTimeout(() => {
                        // Single Tap -> Play/Pause
                        const player = window.reelsObserver.players[videoId];
                        if (player) player.togglePlay();
                        tapCount = 0;
                    }, 300); // 300ms window to wait and see if a second tap happens
                } else if (tapCount === 2) {
                    // Double Tap -> Toggle Theater Mode
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

                // Add physical resistance
                const currentX = deltaX * 0.85;
                const currentY = deltaY * 0.85;
                // Figma animation adds slight rotation as it drags
                const currentRot = currentX * 0.05;

                // Keep the -50% -50% centering from CSS, then add our drag transform
                element.style.transform = `translate(calc(-50% + ${currentX}px), calc(-50% + ${currentY}px)) rotate(${currentRot}deg)`;
            };

            const endSwipe = (x, y) => {
                if (!isSwiping || touchStartX === null) return;
                isSwiping = false;
                const deltaX = x - touchStartX;
                const deltaY = y - touchStartY;
                touchStartX = null;

                // If swiped far enough in any direction
                if (Math.abs(deltaX) > 80 || Math.abs(deltaY) > 80) {
                    // Determine direction based on where they swiped
                    const flyX = deltaX > 0 ? 1000 : -1000;
                    const rotate = deltaX > 0 ? 25 : -25;
                    
                    element.style.transition = 'transform 0.45s cubic-bezier(0.1, 0.7, 0.1, 1)';
                    element.style.transform = `translate(calc(-50% + ${flyX}px), calc(-50% - 800px)) rotate(${rotate}deg)`;

                    // Tell Blazor to completely remove this element from the list
                    setTimeout(() => {
                        if (window.reelsObserver.dotNetRef) {
                            window.reelsObserver.dotNetRef.invokeMethodAsync('ProcessSwipeEndFromJS', deltaX, 0, 0, 0, videoId);
                        }
                        
                        // IMPORTANT: Clear the fly-away styles!
                        // When Blazor inserts this element back at the start of the list to loop it, 
                        // we don't want it to remain 1000px off screen. 
                        element.style.transition = 'none';
                        element.style.transform = '';
                        
                    }, 400);
                } else {
                    // Didn't swipe far enough, snap back to center
                    element.style.transition = 'transform 0.5s cubic-bezier(0.2, 1.2, 0.3, 1)';
                    element.style.transform = '';
                }
            };

            // Bind Touch
            element.addEventListener('touchstart', (e) => startSwipe(e.touches[0].clientX, e.touches[0].clientY), { passive: true });
            element.addEventListener('touchmove', (e) => {
                // Prevent vertical page scrolling while swiping cards, but only if the event can be canceled
                if (isSwiping && e.cancelable) e.preventDefault();
                moveSwipe(e.touches[0].clientX, e.touches[0].clientY);
            }, { passive: false });
            element.addEventListener('touchend', (e) => {
                if (e.changedTouches.length > 0) endSwipe(e.changedTouches[0].clientX, e.changedTouches[0].clientY);
            });

            // Bind Mouse (for desktop testing)
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

        // Optionally destroy Plyr instance to free memory
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

        // Destroy all Plyr instances
        for (const videoId in this.players) {
            this.players[videoId].destroy();
        }
        this.players = {};

        this.dotNetRef = null;
    },

    togglePlayback: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            player.togglePlay();
            if (player.muted) {
                player.muted = false; // Unmute on explicit user interaction
            }
        }
    },

    playVideo: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            // Respect the user's global mute choice
            player.muted = window.reelsObserver.globalMuted;
            if (!window.reelsObserver.globalMuted) {
                player.volume = 1;
            }
            
            let playPromise = player.play();
            if (playPromise !== undefined) {
                playPromise.catch(e => {
                    // If the browser blocks unmuted autoplay, fallback to muted so the video still plays
                    if (!player.muted) {
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