window.manyControlJs = {
    downloadFile: function (filename, content, mimeType) {
        const blob = new Blob([content], { type: mimeType || 'application/json;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    triggerFileInput: function (elementId) {
        const elem = document.getElementById(elementId);
        if (elem) {
            elem.click();
        }
    },

    isIos: function () {
        const userAgent = window.navigator.userAgent.toLowerCase();
        return /iphone|ipad|ipod/.test(userAgent);
    },

    isStandalone: function () {
        return ('standalone' in window.navigator && window.navigator.standalone) ||
               window.matchMedia('(display-mode: standalone)').matches;
    },

    vibrate: function (duration) {
        if ('vibrate' in navigator) {
            navigator.vibrate(duration || 40);
        }
    }
};
