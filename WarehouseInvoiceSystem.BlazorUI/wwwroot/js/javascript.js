globalThis.wisAuth = {
    submitForm: function (formId) {
        const form = document.getElementById(formId);
        if (form) form.submit();
    }
};

globalThis.fileDownloadHelper = {
    downloadFileFromStream: async function (fileName, contentStreamReference) {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    }
};

globalThis.wisAutocomplete = {
    _listeners: new Map(),

    attachScrollListener: function (elementId, dotnetRef, thresholdPx) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        if (this._listeners.has(elementId)) {
            el.removeEventListener('scroll', this._listeners.get(elementId));
            this._listeners.delete(elementId);
        }

        const handler = function () {
            if (el.scrollTop + el.clientHeight >= el.scrollHeight - thresholdPx) {
                dotnetRef.invokeMethodAsync('OnScrolledToBottom');
            }
        };

        el.addEventListener('scroll', handler);
        this._listeners.set(elementId, handler);
        return elementId;
    },

    removeScrollListener: function (elementId) {
        const handler = this._listeners.get(elementId);
        if (handler) {
            const el = document.getElementById(elementId);
            if (el) el.removeEventListener('scroll', handler);
            this._listeners.delete(elementId);
        }
    }
};

globalThis.wisUtils = {
    /** Focus a Blazor ElementReference (passed as an HTMLElement by the JS interop layer). */
    focusElement: function (el) {
        if (el) el.focus();
    }
};