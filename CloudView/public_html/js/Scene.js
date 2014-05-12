
var settings = {
	elements: {
		loader:    $('#loader'),
		container: $('#container'),
		input:     $('#file-input'),
		about:     $('#info-text'),
		header:    $('#header-text'),
		status:    $('#status-text')
	},
	worker: {
		path: 'js/Worker-FileReader.js'
	},
	loader: {
		chunkSize: 8*1024*1024,
		maxPoints: 1000000
	},
	render: {
		useStats: true,
		colorMode: 'texture',
		colorRamp: 'Elevation1',
		colorValues: 256,
		invertRamp: false,
		pointSize: 1,
		showBounds: true
	},
	display: {
		stats: true,
		about: true,
		header: true,
		status: true
	},
	camera: {
		fov: 45,
		near: 1,
		far: 200000
	}
};

var actions = {
	update: function() {
		if (current) {
			startFile(current.file);
		}
	},
	createChunk: {
		'float(3)': createChunkOld,
		'float(3)s': createChunkOldShader,
		'float(1)': createChunkPackedColor,
		'texture': createChunkTexture
	}
};

var worker = null;
var current = null;

var viewport = Viewport3D.create(settings.elements.container[0], {
	camera: settings.camera,
	render: settings.render2
});
settings.elements.loader.hide();

function init() {
	
	var gui = new dat.GUI();
	
	var f2 = gui.addFolder('Loading');
	f2.add(settings.loader, 'chunkSize', createNamedSizes(256*1024, 10));
	f2.add(settings.loader, 'maxPoints', createNamedMultiples(1000000, [0.5,1,2,3,5,10,20]));
	f2.open();
	
	var f1 = gui.addFolder('Rendering');
	f1.add(settings.render, 'colorMode', Object.keys(actions.createChunk));
	f1.add(settings.render, 'colorRamp', Object.keys(ColorRamp.presets)).onChange(function() {updateColorMap();});
	f1.add(settings.render, 'invertRamp').onChange(function() {updateColorMap();});
	f1.add(settings.render, 'useStats');
	f1.add(settings.render, 'showBounds').onChange(function() {updateShowBounds();});
	f1.add(settings.render, 'pointSize').min(1).max(20);
	f1.open();
	
	var f4 = gui.addFolder('Display');
	f4.add(settings.display, 'stats').onChange(function() {$(viewport.stats.domElement).toggle();});
	f4.add(settings.display, 'about').onChange(function() {settings.elements.about.toggle();});
	f4.add(settings.display, 'header').onChange(function() {settings.elements.header.toggle();});
	f4.add(settings.display, 'status').onChange(function() {settings.elements.status.toggle();});
	//f2.open();
	
	var f3 = gui.addFolder('Actions');
	f3.add(actions, 'update');
	f3.open();

	settings.elements.input[0].addEventListener('change', function(e) {
		if (e.target.files.length > 0) {
			startFile(e.target.files[0]);
		}
	});
}

function startFile(file) {
	
	if (!worker) {
		worker = new Worker(settings.worker.path);
		worker.addEventListener('message', function(e) {
			if (e.data.header) {
				onHeaderMessage(e.data);
			}
			else if (e.data.chunk) {
				onChunkMessage(e.data);
			}
		}, false);
	}
	
	var reset = (current === null || current.file !== file);
	clearInfo(reset);

	current = new LASInfo(file);

	worker.postMessage({
		file: file,
		chunkSize: current.settings.loader.chunkSize
	});
}

function clearInfo(reset) {
	if (current) {
		current = null;
		viewport.clearScene();
		settings.elements.header.text('');
		settings.elements.status.text('');
	}
	if (reset) {
		viewport.camera.position.z = 0;
	}
}

function onHeaderMessage(data) {
	var header = data.header.readObject("LASHeader");
	var stats = null;
	if (data.zstats && data.zstats.byteLength > 0) {
		stats = data.zstats.readObject("Statistics");
	}
	current.setHeader(header, data.chunks, stats);

	updateFileInfo();

	var bounds = createBounds();
	if (current.settings.render.showBounds) {
		viewport.add(bounds);
	}
	
	current.geometry.bounds = bounds;

	if (viewport.camera.position.z === 0) {
		var es = header.extent.size();
		viewport.camera.position.z = Math.max(es.x, es.y) * 2;
	}
}

function onChunkMessage(data) {
	var reader = current.getPointReader(data.chunk);
	var object = actions.createChunk[current.settings.render.colorMode](reader);
	viewport.add(object);
	
	current.geometry.chunks.push(object);

	updateProgress(data);
	if (data.index + 1 === current.chunks) {
		updateComplete();
	}
}

function updateProgress(data) {
	var progress = (100 * (data.index + 1) / current.chunks);
	settings.elements.status.text(String.format('{0}%', ~~progress));
}

function updateComplete() {
	var timeSpan = Date.now() - current.startTime;
	var bps = (current.header.numberOfPointRecords * current.header.pointDataRecordLength) / timeSpan * 1000;
	settings.elements.status.text([
		'points : ' + (~~(current.header.numberOfPointRecords / current.step)).toLocaleString(),
		'chunks : ' + current.chunks,
		'stats  : ' + (current.stats !== null),
		'time   : ' + timeSpan.toLocaleString() + " ms",
		'rate   : ' + bytesToSize(bps) + 'ps'
	].join('\n'));
}

function updateFileInfo() {
	var file = current.file;
	var header = current.header;
	settings.elements.header.text([
		'file   : ' + file.name,
		'system : ' + header.systemIdentifier,
		'gensw  : ' + header.generatingSoftware,
		'size   : ' + bytesToSize(file.size),
		'points : ' + header.numberOfPointRecords.toLocaleString(),
		'lasv   : ' + header.version,
		'vlrs   : ' + header.numberOfVariableLengthRecords,
		'evlrs  : ' + header.numberOfExtendedVariableLengthRecords,
		'format : ' + header.pointDataRecordFormat,
		'length : ' + header.pointDataRecordLength,
		'offset : ' + header.quantization.offset,
		'scale  : ' + header.quantization.scale,
		'extent : ' + header.extent.size()
	].join('\n'));
}

function centerObject(obj, centered) {
	var extent = current.header.extent;
	var mid = extent.size().divideScalar(2).add(extent.min);
	
	if (centered !== true) {
		obj.position.x -= mid.x;
		obj.position.y -= mid.y;
		obj.position.z -= mid.z;
	}

	if (current.settings.render.useStats && current.stats) {
		obj.position.z += (mid.z - current.stats.modeApproximate);
	}
}

function createBounds() {
	var es = current.header.extent.size();
	var cube = new THREE.BoxHelper();
	cube.material.color.setRGB(1, 0, 0);
	cube.scale.set(
		(es.x / 2),
		(es.y / 2),
		(es.z / 2)
	);
	centerObject(cube, true);
	
	return cube;
}

function packColor(c) {
	return (c.r*256.0) + (c.g*256.0*256.0) + (c.b*256.0*256.0*256.0);
}

function updateShowBounds() {
	current.updateRenderSettings();
	
	if (current.settings.render.showBounds)
		viewport.add(current.geometry.bounds);
	else
		viewport.remove(current.geometry.bounds);
}

function updateColorMap() {
	current.updateRenderSettings();
	
	/*for(var i = 0; i < geometry.chunks.length; i++) {
		var obj = geometry.chunks[i];
		obj.material.needsUpdate = true;
	}*/
}

function createChunkTexture(reader) {

	uniforms = {
		zmin:     { type: "f", value: current.header.extent.min.z },
		zmax:     { type: "f", value: current.header.extent.max.z },
		texture:  { type: "t", value: current.texture }
	};

	var material = new THREE.ShaderMaterial( {
		uniforms: 		uniforms,
		vertexShader:   document.getElementById('vertexshader2').textContent,
		fragmentShader: document.getElementById('fragmentshader').textContent
	});
	
	var points = reader.filteredPoints;
	
	var geometry = new THREE.BufferGeometry();
	geometry.addAttribute('position', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;

	var kpi = geometry.attributes.position.itemSize;
	var kp = 0;
	
	for (var i = 0; i < points; ++i, kp += kpi) {
		var point = reader.readPoint();
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;
	}

	geometry.computeBoundingSphere();
	
	var obj = new THREE.ParticleSystem(geometry, material);
	
	obj.dynamic = true;
	
	centerObject(obj);
	return obj;
}

function createChunkPackedColor(reader) {

	var attributes = {
		color: {type: 'i', value: null}
	};

	var material = new THREE.ShaderMaterial({
		attributes:     attributes,
		vertexShader:   document.getElementById('vertexshader').textContent,
		fragmentShader: document.getElementById('fragmentshader').textContent
	});
	
	var points = reader.filteredPoints;
	
	var geometry = new THREE.BufferGeometry();
	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 1);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var kpi = geometry.attributes.position.itemSize;
	var kci = geometry.attributes.color.itemSize;
	var kp = 0, kc = 0;
	
	for (var i = 0; i < points; ++i, kp += kpi, kc += kci) {
		var point = reader.readPoint();
		var c = current.colorMap.getColor(point.z);
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;

		colors[kc] = 
			(~~(255 * c.r) << 0) | 
			(~~(255 * c.g) << 8) | 
			(~~(255 * c.b) << 16);
	}

	geometry.computeBoundingSphere();
	
	var obj = new THREE.ParticleSystem(geometry, material);
	centerObject(obj);
	return obj;
}

function createChunkOldShader(reader) {

	var attributes = {
		color: {type: 'c', value: null}
	};

	var material = new THREE.ShaderMaterial({
		attributes:     attributes,
		vertexShader:   document.getElementById('vertexshader3').textContent,
		fragmentShader: document.getElementById('fragmentshader').textContent
	});
	
	var points = reader.filteredPoints;
	
	var geometry = new THREE.BufferGeometry();
	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var kpi = geometry.attributes.position.itemSize;
	var kci = geometry.attributes.color.itemSize;
	var kp = 0, kc = 0;
	for (var i = 0; i < points; ++i, kp += kpi, kc += kci) {
		var point = reader.readPoint();
		var c = current.colorMap.getColor(point.z);
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;

		colors[kc + 0] = c.r;
		colors[kc + 1] = c.g;
		colors[kc + 2] = c.b;
	}

	geometry.computeBoundingSphere();
	
	var obj = new THREE.ParticleSystem(geometry, material);
	centerObject(obj);
	return obj;
}

function createChunkOld(reader) {

	var material = new THREE.ParticleSystemMaterial({
		vertexColors: true,
		size: current.settings.render.pointSize
	});
	
	var points = reader.filteredPoints;
	
	var geometry = new THREE.BufferGeometry();
	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var kpi = geometry.attributes.position.itemSize;
	var kci = geometry.attributes.color.itemSize;
	var kp = 0, kc = 0;
	for (var i = 0; i < points; ++i, kp += kpi, kc += kci) {
		var point = reader.readPoint();
		var c = current.colorMap.getColor(point.z);
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;

		colors[kc + 0] = c.r;
		colors[kc + 1] = c.g;
		colors[kc + 2] = c.b;
	}

	geometry.computeBoundingSphere();
	
	var obj = new THREE.ParticleSystem(geometry, material);
	centerObject(obj);
	return obj;
}

init();