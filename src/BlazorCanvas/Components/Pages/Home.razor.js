const previewAttribute = "data-local-drawing-preview";
const svgNamespace = "http://www.w3.org/2000/svg";

function canvasPoint(surface, event) {
    const bounds = surface.getBoundingClientRect();
    const width = Number(surface.getAttribute("width")) || bounds.width;
    const height = Number(surface.getAttribute("height")) || bounds.height;

    return {
        x: Math.max(0, Math.min(width, (event.clientX - bounds.left) * width / bounds.width)),
        y: Math.max(0, Math.min(height, (event.clientY - bounds.top) * height / bounds.height))
    };
}

function removePreview(surface) {
    surface.querySelector(`[${previewAttribute}]`)?.remove();
}

function previewElement(surface, type) {
    const existing = surface.querySelector(`[${previewAttribute}]`);
    if (existing?.dataset.shape === type) return existing;

    removePreview(surface);
    const element = document.createElementNS(svgNamespace, type === "line" ? "line" : type === "rectangle" ? "rect" : type === "circle" ? "circle" : "polygon");
    element.setAttribute(previewAttribute, "");
    element.dataset.shape = type;
    element.setAttribute("pointer-events", "none");
    element.setAttribute("stroke", "#000000");
    element.setAttribute("stroke-width", "2");
    element.setAttribute("stroke-opacity", "0.7");
    element.setAttribute("fill", type === "line" ? "none" : "#FFFFFF");
    element.setAttribute("fill-opacity", "0.7");
    surface.append(element);
    return element;
}

function updatePreview(surface, gesture, event) {
    const cursor = canvasPoint(surface, event);
    const { press, type } = gesture;
    const element = previewElement(surface, type);
    const x = Math.min(press.x, cursor.x);
    const y = Math.min(press.y, cursor.y);
    const dragWidth = Math.abs(cursor.x - press.x);
    const dragHeight = Math.abs(cursor.y - press.y);

    if (type === "line") {
        element.setAttribute("x1", press.x);
        element.setAttribute("y1", press.y);
        element.setAttribute("x2", cursor.x);
        element.setAttribute("y2", cursor.y);
    } else if (type === "rectangle") {
        element.setAttribute("x", x);
        element.setAttribute("y", y);
        element.setAttribute("width", dragWidth);
        element.setAttribute("height", dragHeight);
    } else if (type === "circle") {
        const canvasWidth = Number(surface.getAttribute("width"));
        const canvasHeight = Number(surface.getAttribute("height"));
        const radius = Math.min(Math.hypot(cursor.x - press.x, cursor.y - press.y), press.x, press.y, canvasWidth - press.x, canvasHeight - press.y);
        element.setAttribute("cx", press.x);
        element.setAttribute("cy", press.y);
        element.setAttribute("r", Math.max(0, radius));
    } else {
        element.setAttribute("points", `${x + dragWidth / 2},${y} ${x},${y + dragHeight} ${x + dragWidth},${y + dragHeight}`);
    }
}

export function attach(surface) {
    if (surface.__localDrawingPreview) return;

    let gesture;
    const start = event => {
        const type = surface.dataset.previewTool;
        if (event.button !== 0 || !type) return;

        gesture = { type, press: canvasPoint(surface, event) };
        surface.setPointerCapture(event.pointerId);
        updatePreview(surface, gesture, event);
    };
    const move = event => {
        if (gesture && (event.buttons & 1) !== 0) updatePreview(surface, gesture, event);
    };
    const finish = event => {
        if (!gesture) return;
        if (surface.hasPointerCapture(event.pointerId)) surface.releasePointerCapture(event.pointerId);
        gesture = undefined;
        removePreview(surface);
    };

    surface.addEventListener("pointerdown", start);
    surface.addEventListener("pointermove", move);
    surface.addEventListener("pointerup", finish);
    surface.addEventListener("pointercancel", finish);
    surface.addEventListener("lostpointercapture", finish);
    surface.__localDrawingPreview = { start, move, finish };
}

export function detach(surface) {
    const handlers = surface.__localDrawingPreview;
    if (!handlers) return;

    surface.removeEventListener("pointerdown", handlers.start);
    surface.removeEventListener("pointermove", handlers.move);
    surface.removeEventListener("pointerup", handlers.finish);
    surface.removeEventListener("pointercancel", handlers.finish);
    surface.removeEventListener("lostpointercapture", handlers.finish);
    delete surface.__localDrawingPreview;
    removePreview(surface);
}
