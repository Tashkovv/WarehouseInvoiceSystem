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
    /**
     * Attaches a scroll listener to the element identified by @elementId.
     * Invokes @dotnetRef.invokeMethodAsync('OnScrolledToBottom') when the user
     * scrolls within @thresholdPx of the bottom.
     * Returns a cleanup token (the listener function) — call removeScrollListener to detach.
     */
    attachScrollListener: function (elementId, dotnetRef, thresholdPx) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        const handler = function () {
            if (el.scrollTop + el.clientHeight >= el.scrollHeight - thresholdPx) {
                dotnetRef.invokeMethodAsync('OnScrolledToBottom');
            }
        };

        el.addEventListener('scroll', handler);
        return elementId; // use as token for removal
    },

    removeScrollListener: function(elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            // Clone to strip all listeners (simplest approach for this use case)
            const clone = el.cloneNode(true);
            el.parentNode.replaceChild(clone, el);
        }
    }
};

globalThis.wisUtils = {
    /** Focus a Blazor ElementReference (passed as an HTMLElement by the JS interop layer). */
    focusElement: function (el) {
        if (el) el.focus();
    }
};