var vertShader = [
	'varying vec3 vColor;',
    'void main() {',
		'gl_PointSize = 20.0;',
        'gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0);',
		'vColor = vec3(position.z / -100.0, 0.0, 1.0);',
    '}'
].join('\n');

var fragShader = [
	'varying vec3 vColor;',
    'void main() {',
        'gl_FragColor = vec4(vColor, 1.0);',
    '}'
].join('\n');

var uniforms = {
	//texture1: { type: "t", value: THREE.ImageUtils.loadTexture("texture.jpg") }
};

var shaderMaterial = new THREE.ShaderMaterial({
	//uniforms: uniforms,
	vertexShader: vertShader,
	fragmentShader: fragShader
});