const previewAttribute = "data-local-drawing-preview";

function removePreview(surface) {
    surface.querySelector(`[${previewAttribute}]`)?.remove();
}

export function attach(surface) {
    if (surface.__localDrawingPreview) return;

    removePreview(surface);

    let capturedPointerId;
    const start = event => {
        const type = surface.dataset.previewTool;
        if (event.button !== 0 || !type) return;

        surface.setPointerCapture(event.pointerId);
        capturedPointerId = event.pointerId;
    };
    const finish = event => {
        if (capturedPointerId === undefined) return;
        if (surface.hasPointerCapture(event.pointerId)) surface.releasePointerCapture(event.pointerId);
        capturedPointerId = undefined;
        removePreview(surface);
    };

    surface.addEventListener("pointerdown", start);
    surface.addEventListener("pointerup", finish);
    surface.addEventListener("pointercancel", finish);
    surface.addEventListener("lostpointercapture", finish);
    surface.__localDrawingPreview = { start, finish };
}

export function detach(surface) {
    const handlers = surface.__localDrawingPreview;
    if (!handlers) return;

    surface.removeEventListener("pointerdown", handlers.start);
    surface.removeEventListener("pointerup", handlers.finish);
    surface.removeEventListener("pointercancel", handlers.finish);
    surface.removeEventListener("lostpointercapture", handlers.finish);
    delete surface.__localDrawingPreview;
    removePreview(surface);
}
