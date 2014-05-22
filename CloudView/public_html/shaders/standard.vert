precision mediump float;

uniform float size;
attribute vec3 color;
varying vec3 vColor;

void main() {
	gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
	gl_PointSize = size;
	//vec4 mvPosition = modelViewMatrix * vec4( position, 1.0 );
	//gl_Position = projectionMatrix * mvPosition;
	//gl_PointSize = size * ( 300.0 / length( mvPosition.xyz ) )/3.0;
	vColor = color;
}