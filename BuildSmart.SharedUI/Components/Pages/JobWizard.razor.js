export function initializeSwipe(element, dotNetHelper) {
    if (!element) return;

    let touchStartX = null;
    let touchStartY = null;
    let touchStartTime = null;
    let isSwiping = false;
    let swipeLocked = false;
    let scrollLocked = false;
    let currentX = 0;
    let currentRot = 0;
    
    // Prevent attaching multiple times if re-initialized
    if (element.__swipeInitialized) return;
    element.__swipeInitialized = true;

    const handleStart = (clientX, clientY) => {
        touchStartX = clientX;
        touchStartY = clientY;
        touchStartTime = Date.now();
        isSwiping = true;
        swipeLocked = false;
        scrollLocked = false;
        // Instantly remove transitions so it sticks to the finger
        element.style.transition = 'none'; 
        
        // Notify Blazor that user interacted (to cancel auto-wiggle)
        dotNetHelper.invokeMethodAsync('NotifyInteraction');
    };

    const handleMove = (clientX, clientY) => {
        if (!isSwiping || touchStartX === null || touchStartY === null) return;
        
        const deltaX = clientX - touchStartX;
        const deltaY = clientY - touchStartY;
        
        if (!swipeLocked && !scrollLocked) {
            // Determine direction
            if (Math.abs(deltaY) > Math.abs(deltaX) && Math.abs(deltaY) > 5) {
                // Vertical scroll detected
                scrollLocked = true;
                isSwiping = false;
                element.style.transform = '';
                return;
            } else if (Math.abs(deltaX) > 5) {
                swipeLocked = true;
            } else {
                return; // Haven't moved enough to determine intent
            }
        }

        if (scrollLocked) return;

        currentX = deltaX * 0.85; // Weight/resistance
        currentRot = Math.max(-10, Math.min(10, currentX * 0.03)); // Cap rotation
        
        element.style.transform = `translateX(${currentX}px) rotate(${currentRot}deg)`;
        element.style.transformOrigin = '50% 100%';
    };

    const handleEnd = (clientX) => {
        if (!isSwiping || touchStartX === null) {
            touchStartX = null;
            touchStartY = null;
            touchStartTime = null;
            swipeLocked = false;
            scrollLocked = false;
            return;
        }
        isSwiping = false;

        let deltaX = 0;
        if (clientX !== null) {
            deltaX = clientX - touchStartX;
        }

        if (!swipeLocked && Math.abs(deltaX) < 10) {
            deltaX = 0;
        }

        let velocity = 0;
        if (touchStartTime) {
            const timeElapsed = Date.now() - touchStartTime;
            if (timeElapsed > 0) {
                velocity = Math.abs(deltaX) / timeElapsed;
            }
        }

        touchStartX = null;
        touchStartY = null;
        touchStartTime = null;
        swipeLocked = false;
        scrollLocked = false;

        // Blazor will handle the animation/snap-back from here based on the result.
        // We pass currentX so Blazor knows exactly where the card is right now 
        // to animate smoothly from that exact spot.
        dotNetHelper.invokeMethodAsync('ProcessSwipeEndFromJS', deltaX, velocity, currentX, currentRot);
    };

    // Touch events
    element.addEventListener('touchstart', (e) => {
        if (e.touches.length > 0) handleStart(e.touches[0].clientX, e.touches[0].clientY);
    }, { passive: true });

    element.addEventListener('touchmove', (e) => {
        if (isSwiping && e.touches.length > 0) {
            handleMove(e.touches[0].clientX, e.touches[0].clientY);
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
    element.addEventListener('mousedown', (e) => handleStart(e.clientX, e.clientY));
    
    // Bind move/up to window so fast drags don't lose focus
    window.addEventListener('mousemove', (e) => {
        if(isSwiping) handleMove(e.clientX, e.clientY);
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
