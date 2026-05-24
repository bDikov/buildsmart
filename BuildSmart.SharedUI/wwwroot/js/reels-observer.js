window.reelsObserver = {
    observer: null,
    dotNetRef: null,
    players: {},
    
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
                        player.muted = true; // Force mute to bypass strict autoplay policies
                        let playPromise = player.play();
                        if (playPromise !== undefined) {
                            playPromise.catch(e => console.log('Autoplay prevented by browser', e));
                        }
                    }
                    // Notify Blazor that this video is now active
                    this.dotNetRef.invokeMethodAsync('OnVideoVisible', videoId);
                } else {
                    if (player) {
                        player.pause();
                        player.currentTime = 0; // Rewind to start
                    }
                }
            });
        }, options);
    },

    observeVideo: function (wrapperId, videoId) {
        const element = document.getElementById(wrapperId);
        const videoElement = document.getElementById(videoId);

        // Initialize Plyr if not already initialized for this video
        if (videoElement && !this.players[videoId]) {
            this.players[videoId] = new Plyr(videoElement, {
                controls: ['play-large', 'play', 'progress', 'current-time', 'mute', 'volume', 'fullscreen'],
                autoplay: false,
                muted: true
            });
        }

        if (element && this.observer) {
            this.observer.observe(element);
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

    // Not strictly needed anymore since Plyr provides a giant play button natively, 
    // but kept just in case for manual overlay integration.
    togglePlayback: function (videoId) {
        const player = this.players[videoId];
        if (player) {
            player.togglePlay();
            if (player.muted) {
                player.muted = false; // Unmute on explicit user interaction
            }
        }
    }
};