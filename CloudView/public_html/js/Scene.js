
var settings = {
	loader: {
		chunkSize: 8*1024*1024
	},
	render: {
		maxPoints: 1000000,
		useStats: true,
		colorRamp: 'Elevation1',
		invertRamp: false,
		pointSize: 1,
		showBounds: true
	},
	camera: {
		fov: 45,
		near: 1,
		far: 200000
	}
};

var actions = {
	update: onUpdateSettings
};

var worker = null;
var current = null;

var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: settings.camera
});
$('#loader').hide();

THREE.Vector3.prototype.toString = function() {
	return String.format('[{0}]', this.toArray().map(function(n) {
		return +n.toFixed(2);
	}).join(', '));
};

function LASInfo(file) {
	this.file = file;
	this.startTime = Date.now();
	
	this.settings = {
		loader: clone(settings.loader),
		render: clone(settings.render)
	};
	
	this.setHeader = function(header, chunks, stats) {
		this.header = header;
		this.chunks = chunks;
		this.stats = stats;
		this.step = Math.ceil(header.numberOfPointRecords / Math.min(this.settings.render.maxPoints, header.numberOfPointRecords));
	};
	
	this.getPointReader = function(buffer) {
		return new PointReader(buffer);
	};
}

function PointReader(buffer) {
	this.reader = new BinaryReader(buffer);
	this.points = (buffer.byteLength / current.header.pointDataRecordLength);
	this.filteredPoints = ~~Math.ceil(this.points / current.step);
	
	this.currentPointIndex = 0;

	this.readPoint = function() {
		if (this.currentPointIndex >= this.points) {
			return null;
		}
		
		this.reader.seek(this.currentPointIndex * current.header.pointDataRecordLength);
		var point = this.reader.readUnquantizedPoint3D(current.header.quantization);
		
		this.currentPointIndex += current.step;
		
		return point;
	};
}

function onUpdateSettings() {
	if (current) {
		startFile(current.file);
	}
}

function initDatGui() {
	var gui = new dat.GUI();
	
	var f2 = gui.addFolder('Loading');
	f2.add(settings.loader, 'chunkSize', createNamedSizes(256*1024, 10));
	f2.open();
	
	var f1 = gui.addFolder('Rendering');
	f1.add(settings.render, 'maxPoints', createNamedMultiples(1000000, [0.5,1,2,3,5,10,20]));
	f1.add(settings.render, 'useStats');
	f1.add(settings.render, 'colorRamp', Object.keys(ColorRamp.presets));
	f1.add(settings.render, 'invertRamp');
	f1.add(settings.render, 'pointSize').min(1).max(20);
	f1.add(settings.render, 'showBounds');
	f1.open();
	
	var f3 = gui.addFolder('Actions');
	f3.add(actions, 'update');
	f3.open();
}

function init() {
	
	initDatGui();

	$('#file-input')[0].addEventListener('change', function(e) {
		if (e.target.files.length > 0) {
			startFile(e.target.files[0]);
		}
	});
}

function startFile(file) {
	
	if (!worker) {
		worker = new Worker('js/Worker-FileReader.js');
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
		$('#header-text').text('');
		$('#status-text').text('');
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

	if (current.settings.render.showBounds) {
		var bounds = createBounds(header.extent);
		viewport.add(bounds);
	}

	if (viewport.camera.position.z === 0) {
		var es = header.extent.size();
		viewport.camera.position.z = Math.max(es.x, es.y) * 2;
	}
}

function onChunkMessage(data) {
	var reader = new PointReader(data.chunk);
	var object = createChunk(reader);
	viewport.add(object);

	updateProgress(data);
	if (data.index + 1 === current.chunks) {
		updateComplete();
	}
}

function updateProgress(data) {
	var progress = (100 * (data.index + 1) / current.chunks);
	$('#status-text').text(String.format('{0}%', ~~progress));
}

function updateComplete() {
	var timeSpan = Date.now() - current.startTime;
	var bps = (current.header.numberOfPointRecords * current.header.pointDataRecordLength) / timeSpan * 1000;
	//$('#status-text').text(String.format('({0} chunks in {1} ms @ {2}ps) 100%', current.chunks, timeSpan.toLocaleString(), bytesToSize(bps)));
	$('#status-text').text([
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
	$('#header-text').text([
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

function createChunk(reader) {

	var points = reader.filteredPoints;
	
	var material = new THREE.ParticleSystemMaterial({vertexColors: true, size: current.settings.render.pointSize});
	//var material = shaderMaterial;
	
	var geometry = new THREE.BufferGeometry();

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets[current.settings.render.colorRamp];
	if (current.settings.render.invertRamp) {
		ramp = ramp.reverse();
	}
	
	var size = current.header.extent.size();
	var min = current.header.extent.min;
	var mid = current.header.extent.size().divideScalar(2).add(min);
	
	var stretch;
	if (current.settings.render.useStats && current.stats) {
		stretch = new StdDevStretch(min.z, current.header.extent.max.z, current.stats, 2);
		mid.z = current.stats.modeApproximate;
	}
	else {
		stretch = new MinMaxStretch(min.z, current.header.extent.max.z);
	}
	var cachedRamp = new CachedColorRamp(ramp, stretch, 1000);
	
	for (var i = 0; i < points; i++) {
		var point = reader.readPoint();
		var x = point.x;
		var y = point.y;
		var z = point.z;
		
		var c = cachedRamp.getColor(z);

		x = (x - mid.x);
		y = (y - mid.y);
		z = (z - mid.z);
		
		var k1 = i * geometry.attributes.position.itemSize;
		positions[k1 + 0] = x;
		positions[k1 + 1] = y;
		positions[k1 + 2] = z;

		var k2 = i * geometry.attributes.color.itemSize;
		colors[k2 + 0] = c.r;
		colors[k2 + 1] = c.g;
		colors[k2 + 2] = c.b;
	}

	geometry.computeBoundingSphere();
	
	return new THREE.ParticleSystem(geometry, material);
}

function createBounds(extent) {
	
	var es = extent.size();
	var cube = new THREE.BoxHelper();
	cube.material.color.setRGB(1, 0, 0);
	cube.scale.set(
		(es.x / 2),
		(es.y / 2),
		(es.z / 2)
	);
	
	if (current.settings.render.useStats && current.stats) {
		var parent = new THREE.Object3D();
		parent.add(cube);
		var mid = extent.size().divideScalar(2).add(extent.min);
		parent.position.z -= (current.stats.modeApproximate - mid.z);
		cube = parent;
	}
	
	return cube;
}

init();