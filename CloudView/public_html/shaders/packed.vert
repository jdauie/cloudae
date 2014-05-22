precision mediump float;

uniform float size;
attribute float color;
varying vec3 vColor;

vec3 unpackColor(float f) {
	vec3 color;
	color.b = floor(f / 256.0 / 256.0);
	color.g = floor((f - color.b * 256.0 * 256.0) / 256.0);
	color.r = floor(f - color.b * 256.0 * 256.0 - color.g * 256.0);
	// normalize
	return color / 256.0;
}

void main() {
	gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
	gl_PointSize = size;
	vColor = unpackColor(color);
}