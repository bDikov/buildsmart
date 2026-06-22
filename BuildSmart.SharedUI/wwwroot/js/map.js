window.addressMap = {
    map: null,
    marker: null,
    dotNetRef: null,
    geocodeTimeout: null,
    
    init: function (elementId, dotNetRef, initialAddress) {
        if (this.map) {
            try {
                this.map.remove();
            } catch (e) {
                console.error("Error removing map:", e);
            }
            this.map = null;
            this.marker = null;
        }
        
        this.dotNetRef = dotNetRef;
        
        // Default coordinates: Sofia, Bulgaria
        const defaultLat = 42.6977;
        const defaultLng = 23.3219;
        
        this.map = L.map(elementId).setView([defaultLat, defaultLng], 13);
        
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);
        
        this.marker = L.marker([defaultLat, defaultLng], { draggable: true }).addTo(this.map);
        
        this.map.on('click', (e) => {
            const latlng = e.latlng;
            this.updateMarkerPosition(latlng.lat, latlng.lng, true);
        });
        
        this.marker.on('dragend', (e) => {
            const latlng = this.marker.getLatLng();
            this.updateMarkerPosition(latlng.lat, latlng.lng, true);
        });
        
        if (initialAddress) {
            this.geocodeAddress(initialAddress);
        }
    },
    
    updateMarkerPosition: function (lat, lng, triggerGeocode) {
        this.marker.setLatLng([lat, lng]);
        this.map.panTo([lat, lng]);
        
        if (triggerGeocode) {
            this.reverseGeocode(lat, lng);
        }
    },
    
    reverseGeocode: function (lat, lng) {
        fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`, {
            headers: {
                'Accept-Language': 'bg,en'
            }
        })
        .then(res => res.json())
        .then(data => {
            const displayName = data.display_name || `${lat}, ${lng}`;
            const isWithinRange = this.isWithinSofiaRange(lat, lng);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnAddressSelectedFromMap', displayName, isWithinRange);
            }
        })
        .catch(err => console.error("Reverse geocoding failed:", err));
    },
    
    geocodeAddress: function (address) {
        if (!address) {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnLocationRangeUpdated', false);
            }
            return;
        }
        
        if (this.geocodeTimeout) {
            clearTimeout(this.geocodeTimeout);
        }
        
        this.geocodeTimeout = setTimeout(() => {
            fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(address)}&limit=1`, {
                headers: {
                    'Accept-Language': 'bg,en'
                }
            })
            .then(res => res.json())
            .then(data => {
                if (data && data.length > 0) {
                    const lat = parseFloat(data[0].lat);
                    const lon = parseFloat(data[0].lon);
                    this.marker.setLatLng([lat, lon]);
                    this.map.setView([lat, lon], 15);
                    
                    const isWithinRange = this.isWithinSofiaRange(lat, lon);
                    if (this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync('OnLocationRangeUpdated', isWithinRange);
                    }
                }
            })
            .catch(err => console.error("Geocoding failed:", err));
        }, 800);
    },

    isWithinSofiaRange: function (lat, lng) {
        const sofiaLat = 42.6977;
        const sofiaLng = 23.3219;
        
        const R = 6371; // Earth's radius in km
        const dLat = (lat - sofiaLat) * Math.PI / 180;
        const dLon = (lng - sofiaLng) * Math.PI / 180;
        
        const a = 
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(sofiaLat * Math.PI / 180) * Math.cos(lat * Math.PI / 180) * 
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
            
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        const distance = R * c;
        
        return distance <= 30; // 30 km
    }
};
