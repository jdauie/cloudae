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
		url:       $('#url-input'),
		urlCmd:    $('#url-cmd'),
		about:     $('#info-text'),
		header:    $('#header-text'),
		status:    $('#status-text'),
		ramp:      $('#ramp')
	},
	shaders: {
	},
	worker: {
		path: 'js/Worker-FileReader.js'
	},
	loader: {
		chunkSize: 4*1024*1024,
		maxPoints: 1000000
	},
	render: {
		useStats: true,
		colorMode: 'rgb',
		colorRamp: 'Elevation1',
		colorValues: 256,
		invertRamp: false,
		pointSize: 1,
		showBounds: true,
		chunkVertices: 1000000
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

var viewport = JACERE.Viewport3D.create(settings.elements.container[0], {
	camera: settings.camera//,
	//render: settings.render2
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
	f2.add(settings.loader, 'chunkSize', JACERE.Util.createNamedSizes(256*1024, 10));
	f2.add(settings.loader, 'maxPoints', JACERE.Util.createNamedMultiples(1000000, [0.5,1,2,3,4,5,6,8,10,12,14,16,18,20]));
	f2.open();
	
	var f3 = gui.addFolder('Actions');
	f3.add(actions, 'update');
	f3.open();
	
	var f1 = gui.addFolder('Rendering');
	f1.add(settings.render, 'colorMode', ['rgb','height']).onChange(function() {current.updateRenderSettings();});
	f1.add(settings.render, 'colorRamp', Object.keys(JACERE.ColorRamp.presets)).onChange(function() {current.updateRenderSettings();});
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
	
	settings.elements.urlCmd[0].addEventListener('click', function(e) {
		var url = settings.elements.url.val();
		if (url) {
			startFile(url);
		}
	});
	
	$('.data-sample').on('click', function(e) {
		startFile(String.format('../data/{0}', $(e.target).data('src')));
	});
	
	//document.addEventListener('mousedown', onDocumentMouseDown, false);
	
	
	
	/*var ambientLight = new THREE.AmbientLight(0xffffff);
	viewport.add(ambientLight);*/

	/*var pointLight = new THREE.PointLight(0xffffff);
	pointLight.intensity = 2;
	pointLight.position.z = 100;
	pointLight.position.y = 700;
	viewport.add(pointLight);*/

	/*var directionalLight = new THREE.DirectionalLight(0xffffff, 1.5);
	directionalLight.position.set(1, 1, 1).normalize();
	viewport.add(directionalLight);*/
	
	
	
	var fragmentShaders = $('script[type="x-shader/x-fragment"]');
	var vertexShaders	= $('script[type="x-shader/x-vertex"]');
	
	var shaders = [];
	
	for (var i = 0; i < fragmentShaders.length; i++) {
		shaders.push(fragmentShaders[i]);
	}
	for (var i = 0; i < vertexShaders.length; i++) {
		shaders.push(vertexShaders[i]);
	}
	
	var loadedShaders = shaders.map(function(shader) {
		var xhr = new XMLHttpRequest();
		xhr.open('GET', shader.src, false);
		xhr.send(null);
		return {
			src: shader.src,
			name: shader.src.replace(/^.*[\\\/]/, ''),
			type: shader.type.replace(/^.*[\-]/, ''),
			shader: xhr.response
		};
	});
	
	for (var i = 0; i < loadedShaders.length; i++) {
		var shader = loadedShaders[i];
		settings.shaders[shader.name] = shader;
	}
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

	current = new JACERE.LASInfo(file);

	worker.postMessage({
		file: file,
		chunkSize: current.settings.loader.chunkSize
	});
}

function clearInfo() {
	if (current) {
		current.dispose();
		current = null;
		viewport.clearScene();
		settings.elements.header.text('');
		settings.elements.status.text('');
	}
}

function onHeaderMessage(data) {
	var header = data.header.readObject("LASHeader", JACERE);
	var tiles = null;
	if (data.tiles && data.tiles.byteLength > 0) {
		tiles = data.tiles.readObject("PointCloudTileSet", JACERE);
	}
	var stats = null;
	if (data.stats && data.stats.byteLength > 0) {
		stats = data.stats.readObject("Statistics", JACERE);
	}
	current.setHeader(header, data.length, tiles, stats);

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

function updateFrustrumRange() {
	if (!current || !current.geometry.bounds)
		return;
	
	// is the center always the origin?
	/*current.geometry.bounds.geometry.computeBoundingSphere();
	var sphereCenter = current.geometry.bounds.geometry.boundingSphere.center;
	sphereCenter.applyMatrix4(current.geometry.bounds.matrixWorld);
	
	var point1 = viewport.camera.position.clone();
	//point1.applyMatrix4(viewport.camera.matrixWorld);
	var point2 = sphereCenter;
	var distance = point1.distanceTo(point2);
	
	//var sphereRadius = current.geometry.bounds.geometry.boundingSphere.radius;
	//var sphereRadius = current.geometry.bounds.scale.length();
	var radius = current.header.extent.size().length() / 2;*/
	
	var distance = viewport.camera.position.distanceTo(new THREE.Vector3());
	
	viewport.camera.near = Math.max(1, distance - current.radius);
	viewport.camera.far = distance + current.radius;
	viewport.camera.updateProjectionMatrix();
}

function onProgressMessage(ratio) {
	settings.elements.status.html(String.format('<span id="status-progress">{0}%</span>', ~~(100 * ratio)));
	updateProgress(ratio);
}

function onChunkMessage(data) {
	
	// testing
	if (false && viewport.scene.children.length > 40) {
		viewport.clearScene();
	}
	
	current.setTime();
	
	var render1 = Date.now();
	
	var pointsRemaining = data.pointCount;
	var group = new THREE.Object3D();
	while (pointsRemaining > 0) {
		var points = Math.min(current.settings.render.chunkVertices, pointsRemaining);
		var reader = current.getPointReader(data.buffer, data.pointCount - pointsRemaining, points);
		
		pointsRemaining -= points;
		
		var node = createChunkPackedColor(reader, points);
		group.add(node);
		current.geometry.chunks.push(node);
	}
	centerObject(group);
	viewport.add(group);
	
	console.log(String.format('time[geometry]: {0} ms', (Date.now() - render1).toLocaleString()));
	
	if (current.geometry.progress) {
		viewport.remove(current.geometry.progress);
		current.geometry.progress = null;
		
		// testing!
		if (false) {
			//viewport.remove(group);
			var testTileCount = 40;
			var startingTileIndex = ~~(current.tiles.validTileCount / 2) - ~~(testTileCount / 2);
			var regionPointOffset = current.tiles.getValidTile(startingTileIndex).pointOffset;
			var regionPointCount = 0;
			for (var i = 0; i < testTileCount; i++) {
				var tile = current.tiles.getValidTile(startingTileIndex + i);
				regionPointCount += (tile.pointCount - tile.lowResCount);
			}
			worker.postMessage({
				pointOffset: regionPointOffset,
				pointCount: regionPointCount
			});
		}
		if (false) {
			//viewport.remove(group);
			var testTileCount = 40;
			var startingTileIndex = ~~(current.tiles.validTileCount / 2) - ~~(testTileCount / 2);
			for (var i = 0; i < testTileCount; i++) {
				var tile = current.tiles.getValidTile(startingTileIndex + i);
				worker.postMessage({
					pointOffset: tile.pointOffset,
					pointCount: (tile.pointCount - tile.lowResCount)
				});
			}
		}
		if (false) {
			//viewport.remove(group);
			var testTileRadius = 3;
			var midX = ~~(current.tiles.cols / 2);
			var midY = ~~(current.tiles.rows / 2);
			var startX = midX - testTileRadius;
			var startY = midY - testTileRadius;
			var endX = midX + testTileRadius;
			var endY = midY + testTileRadius;
			for (var y = startY; y < endY; y++) {
				for (var x = startX; x < endX; x++) {
					//if (Math.sqrt((x-midX)*(x-midX)+(y-midY)*(y-midY)) > testTileRadius)
					//	continue;
					var tile = current.tiles.getTile(y, x);
					if (tile) {
						worker.postMessage({
							pointOffset: tile.pointOffset,
							pointCount: (tile.pointCount - tile.lowResCount)
						});
					}
				}
			}
		}
		if (false) {
			//viewport.remove(group);
			for (var y = 0; y < current.tiles.rows; y+=2) {
				for (var x = 0; x < current.tiles.cols; x+=2) {
					var tile = current.tiles.getTile(y, x);
					if (tile) {
						worker.postMessage({
							pointOffset: tile.pointOffset,
							pointCount: (tile.pointCount - tile.lowResCount)
						});
					}
				}
			}
		}
		if (false) {
			viewport.remove(group);
			for (var i = 0; i < current.tiles.validTileCount; i++) {
				var tile = current.tiles.getValidTile(i);
				worker.postMessage({
					pointOffset: tile.pointOffset,
					pointCount: (tile.pointCount - tile.lowResCount)
				});
			}
		}
	}
	else {
		//viewport.clearScene();
		//viewport.add(group);
	}
	updateShowBounds();
	
	if (current.tiles) {
		updateCompleteTiled();
	}
	else {
		updateCompleteThinned(data.pointCount);
	}
}

function updateCompleteTiled() {
	var timeSpan = current.getLoadTime();
	settings.elements.status.text([
		'points : ' + current.tiles.lowResCount.toLocaleString(),
		'tiles  : ' + current.tiles.validTileCount.toLocaleString(),
		'time   : ' + timeSpan.toLocaleString() + " ms"
	].join('\n'));
}

function updateCompleteThinned(count) {
	var timeSpan = current.getLoadTime();
	var bps = (current.header.numberOfPointRecords * current.header.pointDataRecordLength) / timeSpan * 1000;
	settings.elements.status.text([
		'points : ' + count.toLocaleString(),
		'time   : ' + timeSpan.toLocaleString() + " ms",
		'rate   : ' + JACERE.Util.bytesToSize(bps) + 'ps'
	].join('\n'));
}

function updateFileInfo() {
	var header = current.header;
	settings.elements.header.text([
		'file   : ' + current.name,
		'system : ' + header.systemIdentifier,
		'gensw  : ' + header.generatingSoftware,
		'size   : ' + JACERE.Util.bytesToSize(current.fileSize),
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
	if (current.geometry.progress)
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
	
	current.updateRenderSettings(false);
	
	if (current.settings.render.showBounds)
		viewport.add(current.geometry.bounds);
	else
		viewport.remove(current.geometry.bounds);
}

function createChunkPackedColor(reader) {

	var points = reader.points;
	var geometry = new THREE.BufferGeometry();
	
	var positions = new Float32Array(points * 3);
	var colors = new Float32Array(points * 1);
	
	geometry.attributes = {
		position: { itemSize: 3, array: positions },
		color:    { itemSize: 1, array: colors }
	};

	var kp = 0;
	
	// if we are in texture mode, don't bother setting the colors, just allocate them?
	for (var i = 0; i < points; ++i, kp += 3) {
		var point = reader.readPoint();
		
		positions[kp + 0] = point.x;
		positions[kp + 1] = point.y;
		positions[kp + 2] = point.z;
		
		colors[i] = point.color;
	}

	geometry.computeBoundingSphere();
	
	var obj = new THREE.ParticleSystem(geometry, current.material);
	obj.sourceReader = reader;
	return obj;
}

function updateChunkPackedColor(obj) {

	var reader = obj.sourceReader;
	reader.reset();

	// check whether it needs to be updated?
	obj.material = current.material;
	
	var geometry = obj.geometry;
	var colors = geometry.attributes.color.array;
	
	for (var i = 0; i < reader.points; ++i) {
		colors[i] = reader.readPointColor();
	}

	geometry.attributes.color.needsUpdate = true;
}

init();