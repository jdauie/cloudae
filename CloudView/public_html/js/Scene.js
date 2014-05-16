/**
 * Copyright (c) 2014, Joshua Morey <josh@joshmorey.com>
 * 
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 * 
 * @package CloudView
 * @author Joshua Morey <josh@joshmorey.com>
 * @copyright 2014 Joshua Morey <josh@joshmorey.com>
 * @license http://opensource.org/licenses/ISC
 */

var settings = {
	elements: {
		loader:    $('#loader'),
		container: $('#container'),
		files:     $('#file-input'),
		//url:       $('#url-input'),
		//urlCmd:    $('#url-cmd'),
		about:     $('#info-text'),
		header:    $('#header-text'),
		status:    $('#status-text')
	},
	worker: {
		path: 'js/Worker-FileReader.js'
	},
	loader: {
		chunkSize: 8*1024*1024,
		maxPoints: 1000000,
		colorMode: 'texture'
	},
	render: {
		useStats: true,
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
		'float(1)': createChunkPackedColor,
		'texture': createChunkTexture
	}
};

var worker = null;
var current = null;
var reset = true;


function onDocumentMouseDown(event) {
	if (!current || !current.tiles)
		return;
	
	//event.preventDefault();
	//centroidSphere.visible=true;mousedown=true;

	// find intersections
	var mousex = (event.clientX / window.innerWidth) * 2 - 1;
	var mousey = -(event.clientY / window.innerHeight) * 2 + 1;

	var vector = new THREE.Vector3(mousex, mousey, 1);
	var raycaster = new THREE.Raycaster(viewport.camera.position, vector.sub(viewport.camera.position).normalize());
	
	var intersections = [];
	var sphere = new THREE.Sphere();
	for (var i = 0; i < current.geometry.chunks.length; i++) {
		var object = current.geometry.chunks[i];
		var boundingSphere = object.geometry.boundingSphere;
		
		sphere.copy(boundingSphere);
		sphere.applyMatrix4(object.matrixWorld);

		if (raycaster.ray.isIntersectionSphere(sphere) === true) {
			intersections.push(current.tiles.getValidTile(i));
		}
	}
	
	$('#info-text').text(intersections.join('\n'));
	
	//var vector = new THREE.Vector3( mouse.x, mouse.y, 0.5 );
	//projector.unprojectVector( vector, camera );
	//var ray = new THREE.Ray( camera.position, vector.subSelf( camera.position ).normalize() );
}

var viewport = Viewport3D.create(settings.elements.container[0], {
	camera: settings.camera,
	render: settings.render2
});
settings.elements.loader.hide();

function init() {
	//»«
	settings.elements.about.html([
		'<a href="https://github.com/mrdoob/three.js">three.js</a> — <a href="http://blog.jacere.net/2014/05/webgl-las-viewer">WebGL LAS Viewer DEMO</a> — <a href="http://blog.jacere.net">jdauie</a>',
		'Supports <a href="http://blog.jacere.net/cloudae">CloudAE</a> SATR2 records for instant low-res.',
		'( <a href="data/CloudAE.7z">download</a> » process » %temp%\\jacere\\cache )'
	].join('\n'));
	
	var gui = new dat.GUI();
	
	var f2 = gui.addFolder('Loading');
	f2.add(settings.loader, 'chunkSize', createNamedSizes(256*1024, 10));
	f2.add(settings.loader, 'maxPoints', createNamedMultiples(1000000, [0.5,1,2,3,4,5,6,8,10,12,14,16,18,20]));
	f2.add(settings.loader, 'colorMode', Object.keys(actions.createChunk));
	f2.open();
	
	var f3 = gui.addFolder('Actions');
	f3.add(actions, 'update');
	f3.open();
	
	var f1 = gui.addFolder('Rendering');
	f1.add(settings.render, 'colorRamp', Object.keys(ColorRamp.presets)).onChange(function() {current.updateRenderSettings();});
	f1.add(settings.render, 'invertRamp').onChange(function() {current.updateRenderSettings();});
	f1.add(settings.render, 'useStats').onChange(function() {current.updateRenderSettings();});
	f1.add(settings.render, 'showBounds').onChange(function() {updateShowBounds();});
	f1.add(settings.render, 'pointSize').min(1).max(20).onChange(function() {current.updateRenderSettings();});
	f1.open();
	
	var f4 = gui.addFolder('Display');
	f4.add(settings.display, 'stats').onChange(function() {$(viewport.stats.domElement).toggle();});
	f4.add(settings.display, 'about').onChange(function() {settings.elements.about.toggle();});
	f4.add(settings.display, 'header').onChange(function() {settings.elements.header.toggle();});
	f4.add(settings.display, 'status').onChange(function() {settings.elements.status.toggle();});
	//f2.open();

	settings.elements.files[0].addEventListener('change', function(e) {
		if (e.target.files.length > 0) {
			startFile(e.target.files[0]);
		}
	});
	
	/*settings.elements.urlCmd[0].addEventListener('click', function(e) {
		var url = settings.elements.url.val();
		if (url) {
			startFile(url);
		}
	});
	
	var sampleListener = function(e) {
		startFile(String.format('../data/{0}', e.target.value));
	};*/
	
	//document.addEventListener('mousedown', onDocumentMouseDown, false);
	
	/*var ambientLight = new THREE.AmbientLight(0xffffff);
	viewport.add(ambientLight);

	var pointLight = new THREE.PointLight(0xffffff);
	pointLight.intensity = 2;
	pointLight.position.z = 100;
	pointLight.position.y = 700;
	viewport.add(pointLight);

	var directionalLight = new THREE.DirectionalLight(0xffffff, 1.5);
	directionalLight.position.set(1, 1, 2);
	directionalLight.position.normalize();
	viewport.add(directionalLight);*/
}

function startFile(file) {
	
	if (!worker) {
		worker = new Worker(settings.worker.path);
		worker.addEventListener('message', function(e) {
			if (e.data.header) {
				onHeaderMessage(e.data);
			}
			else if (e.data.buffer) {
				onChunkMessage(e.data);
			}
			else if (e.data.progress) {
				onProgressMessage(e.data.progress);
			}
		}, false);
	}
	
	reset = (current === null || current.file !== file);
	clearInfo();

	current = new LASInfo(file);

	worker.postMessage({
		file: file,
		chunkSize: current.settings.loader.chunkSize
	});
}

function clearInfo() {
	if (current) {
		current = null;
		viewport.clearScene();
		settings.elements.header.text('');
		settings.elements.status.text('');
	}
}

function onHeaderMessage(data) {
	var header = data.header.readObject("LASHeader");
	var tiles = null;
	if (data.tiles && data.tiles.byteLength > 0) {
		tiles = data.tiles.readObject("PointCloudTileSet");
	}
	var stats = null;
	if (data.stats && data.stats.byteLength > 0) {
		stats = data.stats.readObject("Statistics");
	}
	current.setHeader(header, tiles, stats);

	updateFileInfo();

	current.geometry.bounds = createBounds();
	viewport.add(current.geometry.bounds);

	current.geometry.progress = createProgress();
	viewport.add(current.geometry.progress);
	
	if (reset) {
		var es = header.extent.size();
		viewport.controls.reset();
		viewport.camera.position.set(0, 0, Math.max(es.x, es.y) * 2);
	}
	
	if (current.tiles) {
		// request low-res points
		worker.postMessage({
			pointOffset: current.tiles.lowResOffset,
			pointCount: current.tiles.lowResCount
		});
	}
	else {
		// request thinned points
		worker.postMessage({
			pointOffset: 0,
			pointCount: current.header.numberOfPointRecords,
			step: current.step
		});
	}
}

function onProgressMessage(ratio) {
	settings.elements.status.html(String.format('<span id="status-progress">{0}%</span>', ~~(100 * ratio)));
	updateProgress(ratio);
}

function onChunkMessage(data) {
	var reader = current.getPointReader(data.buffer, data.filteredCount);
	var pointsRemaining = reader.points;
	var group = new THREE.Object3D();
	while (pointsRemaining > 0) {
		var points = Math.min(2000000, pointsRemaining);
		pointsRemaining -= points;
		
		var node = actions.createChunk[current.settings.loader.colorMode](reader, points);
		group.add(node);
		current.geometry.chunks.push(node);
	}
	centerObject(group);
	viewport.add(group);
	
	viewport.remove(current.geometry.progress);
	current.geometry.progress = null;
	updateShowBounds();
	
	if (current.tiles) {
		updateCompleteTiled();
	}
	else {
		updateCompleteThinned(reader.points);
	}
}

function updateCompleteTiled() {
	var timeSpan = Date.now() - current.startTime;
	settings.elements.status.text([
		'points : ' + current.tiles.lowResCount.toLocaleString(),
		'tiles  : ' + current.tiles.validTileCount.toLocaleString(),
		'time   : ' + timeSpan.toLocaleString() + " ms"
	].join('\n'));
}

function updateCompleteThinned(count) {
	var timeSpan = Date.now() - current.startTime;
	var bps = (current.header.numberOfPointRecords * current.header.pointDataRecordLength) / timeSpan * 1000;
	settings.elements.status.text([
		'points : ' + count.toLocaleString(),
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
		'scale  : ' + header.quantization.scale.toStringLong(),
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

function updateProgress(ratio) {
	current.geometry.progress.scale.set(ratio, ratio, ratio);
}

function createProgress() {
	var es = current.header.extent.size();
	var geometry = new THREE.CubeGeometry(es.x, es.y, es.z);
	var material = new THREE.MeshBasicMaterial({
		color: 0x0099ff
	});
	var box = new THREE.Mesh(geometry, material);
	box.scale.set(0, 0, 0);
	centerObject(box, true);
	
	return box;
}

function createBounds() {
	var es = current.header.extent.size();
	var box = new THREE.BoxHelper();
	box.material.color.setRGB(1, 0, 0);
	box.scale.set(
		(es.x / 2),
		(es.y / 2),
		(es.z / 2)
	);
	centerObject(box, true);
	
	return box;
}

function packColor(c) {
	return (c.r*256.0) + (c.g*256.0*256.0) + (c.b*256.0*256.0*256.0);
}

function updateShowBounds() {
	if (current.geometry.progress)
		return;
	
	current.updateRenderSettings();
	
	if (current.settings.render.showBounds)
		viewport.add(current.geometry.bounds);
	else
		viewport.remove(current.geometry.bounds);
}

function createChunkTexture(reader, points) {

	if (!current.material) {
		var uniforms = {
			size:     { type: "f", value: current.settings.render.pointSize },
			zmin:     { type: "f", value: current.header.extent.min.z },
			zmax:     { type: "f", value: current.header.extent.max.z },
			texture:  { type: "t", value: current.texture }
		};

		current.material = new THREE.ShaderMaterial( {
			uniforms: 		uniforms,
			vertexShader:   document.getElementById('vertexshader2').textContent,
			fragmentShader: document.getElementById('fragmentshader').textContent
		});
	}
	
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

	return new THREE.ParticleSystem(geometry, current.material);
}

function createChunkPackedColor(reader, points) {

	if (!current.material) {
		var uniforms = {
			size: { type: "f", value: current.settings.render.pointSize }
		};

		var attributes = {
			color: {type: 'i', value: null}
		};

		current.material = new THREE.ShaderMaterial({
			uniforms:       uniforms,
			attributes:     attributes,
			vertexShader:   document.getElementById('vertexshader').textContent,
			fragmentShader: document.getElementById('fragmentshader').textContent
		});
	}
	
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
		var c = point.r
			? new THREE.Color(point.r, point.g, point.b)
			: current.colorMap.getColor(point.z);
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;

		colors[kc] = 
			(~~(255 * c.r) << 0) | 
			(~~(255 * c.g) << 8) | 
			(~~(255 * c.b) << 16);
	}

	geometry.computeBoundingSphere();
	
	return new THREE.ParticleSystem(geometry, current.material);
}

function createChunkOld(reader, points) {

	if (!current.material) {
		current.material = new THREE.ParticleSystemMaterial({
			vertexColors: true,
			size: current.settings.render.pointSize
			//sizeAttentuation: false
		});
	}
	
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
		var c = point.r
			? new THREE.Color(point.r, point.g, point.b)
			: current.colorMap.getColor(point.z);
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;

		colors[kc + 0] = c.r;
		colors[kc + 1] = c.g;
		colors[kc + 2] = c.b;
	}

	geometry.computeBoundingSphere();
	
	return new THREE.ParticleSystem(geometry, current.material);
}

function updateChunkOld(obj) {

	if (current.header.pointDataRecordFormat === 2)
		return;
	
	var geometry = obj.geometry;
	
	var points = (geometry.attributes.position.array.length / 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var kpi = geometry.attributes.position.itemSize;
	var kci = geometry.attributes.color.itemSize;
	var kp = 0, kc = 0;
	for (var i = 0; i < points; ++i, kp += kpi, kc += kci) {
		var z = positions[kp + 2];
		var c = current.colorMap.getColor(z);
		
		if (kci === 1) {
			colors[kc] = 
				(~~(255 * c.r) << 0) | 
				(~~(255 * c.g) << 8) | 
				(~~(255 * c.b) << 16);
		}
		else {
			colors[kc + 0] = c.r;
			colors[kc + 1] = c.g;
			colors[kc + 2] = c.b;
		}
	}

	geometry.attributes.color.needsUpdate = true;
}

init();