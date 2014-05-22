precision mediump float;
precision lowp sampler2D;

uniform float size;
uniform sampler2D texture;
uniform float zmin;
uniform float zscale;
varying vec3 vColor;

void main() {
	gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
	gl_PointSize = size;
	vColor = texture2D(texture, vec2(0, (position.z - zmin) * zscale)).xyz;
}