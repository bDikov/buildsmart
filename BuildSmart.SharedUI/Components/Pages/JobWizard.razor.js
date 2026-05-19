export function initializeSwipe(element, dotNetHelper) {
    if (!element) return;

    let touchStartX = null;
    let touchStartTime = null;
    let isSwiping = false;
    let currentX = 0;
    let currentRot = 0;
    
    // Prevent attaching multiple times if re-initialized
    if (element.__swipeInitialized) return;
    element.__swipeInitialized = true;

    const handleStart = (clientX) => {
        touchStartX = clientX;
        touchStartTime = Date.now();
        isSwiping = true;
        // Instantly remove transitions so it sticks to the finger
        element.style.transition = 'none'; 
        
        // Notify Blazor that user interacted (to cancel auto-wiggle)
        dotNetHelper.invokeMethodAsync('NotifyInteraction');
    };

    const handleMove = (clientX) => {
        if (!isSwiping || touchStartX === null) return;
        
        const rawDelta = clientX - touchStartX;
        currentX = rawDelta * 0.85; // Weight/resistance
        currentRot = Math.max(-10, Math.min(10, currentX * 0.03)); // Cap rotation
        
        element.style.transform = `translateX(${currentX}px) rotate(${currentRot}deg)`;
        element.style.transformOrigin = '50% 100%';
    };

    const handleEnd = (clientX) => {
        if (!isSwiping || touchStartX === null) return;
        isSwiping = false;

        let deltaX = 0;
        if (clientX !== null) {
            deltaX = clientX - touchStartX;
        }

        let velocity = 0;
        if (touchStartTime) {
            const timeElapsed = Date.now() - touchStartTime;
            if (timeElapsed > 0) {
                velocity = Math.abs(deltaX) / timeElapsed;
            }
        }

        touchStartX = null;
        touchStartTime = null;

        // Blazor will handle the animation/snap-back from here based on the result.
        // We pass currentX so Blazor knows exactly where the card is right now 
        // to animate smoothly from that exact spot.
        dotNetHelper.invokeMethodAsync('ProcessSwipeEndFromJS', deltaX, velocity, currentX, currentRot);
    };

    // Touch events
    element.addEventListener('touchstart', (e) => {
        if (e.touches.length > 0) handleStart(e.touches[0].clientX);
    }, { passive: true });

    element.addEventListener('touchmove', (e) => {
        if (isSwiping && e.touches.length > 0) {
            handleMove(e.touches[0].clientX);
            // Cannot preventDefault if passive: true. But we want vertical scroll to work natively anyway.
        }
    }, { passive: true });

    element.addEventListener('touchend', (e) => {
        if (e.changedTouches.length > 0) {
            handleEnd(e.changedTouches[0].clientX);
        } else {
            handleEnd(null);
        }
    });

    // Mouse events
    element.addEventListener('mousedown', (e) => handleStart(e.clientX));
    
    // Bind move/up to window so fast drags don't lose focus
    window.addEventListener('mousemove', (e) => {
        if(isSwiping) handleMove(e.clientX);
    }); 
    window.addEventListener('mouseup', (e) => {
        if(isSwiping) handleEnd(e.clientX);
    });
}

export function resetSwipeStyle(element) {
    if (!element) return;
    // We let Blazor handle the transition and transform during snap-backs and fly-aways
    element.style.transform = '';
    element.style.transition = '';
}
