window.reelsObserver = {
    observer: null,
    dotNetRef: null,
    
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
                const videoElement = entry.target.querySelector('video');

                if (entry.isIntersecting) {
                    if (videoElement) {
                        videoElement.play().catch(e => console.log('Autoplay prevented', e));
                    }
                    // Notify Blazor that this video is now active
                    this.dotNetRef.invokeMethodAsync('OnVideoVisible', videoId);
                } else {
                    if (videoElement) {
                        videoElement.pause();
                        videoElement.currentTime = 0; // Rewind to start
                    }
                }
            });
        }, options);
    },

    observeVideo: function (elementId) {
        const element = document.getElementById(elementId);
        if (element && this.observer) {
            this.observer.observe(element);
        }
    },

    unobserveVideo: function (elementId) {
        const element = document.getElementById(elementId);
        if (element && this.observer) {
            this.observer.unobserve(element);
        }
    },

    dispose: function () {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
        this.dotNetRef = null;
    }
};