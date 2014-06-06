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
	vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
	gl_Position = projectionMatrix * mvPosition;
	gl_PointSize = size;
	//gl_PointSize = size * (100.0 / length(mvPosition.xyz));
	vColor = unpackColor(color);
}