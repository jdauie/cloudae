
var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: {
		fov:  45,
		near: 1,
		far:  200000
	}
});
$('#loader').hide();

THREE.Vector3.prototype.toString = function() {
	return String.format('[{0}]', this.toArray().map(function(n) {
		return +n.toFixed(2);
	}).join(', '));
};

function bytesToSize(bytes) {
   var k = 1024;
   var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
   if (bytes === 0) return '0 Bytes';
   var i = parseInt(Math.floor(Math.log(bytes) / Math.log(k)),10);
   return (bytes / Math.pow(k, i)).toPrecision(3) + ' ' + sizes[i];
}

var worker;
var file;
var statsZ;
var header;
var chunks;
var pointStep;
var startTime;
var resetCamera;

var loaderSettings = {
	chunkSize: 8*1024*1024
};

var renderSettings = {
	maxPoints: 1000000,
	colorRamp: 'Elevation1',
	invertRamp: false,
	pointSize: 1,
	showBounds: true
};

var actionMethods = {
	update: onUpdateSettings
};

function onUpdateSettings() {
	var fileInput = $('#file-input')[0];
	fileInput.dispatchEvent(new Event('change'));
}

function init() {
	
	var gui = new dat.GUI();
	var f2 = gui.addFolder('Loading');
	f2.add(loaderSettings, 'chunkSize', {
		'128 MB': 128*1024*1024,
		'64 MB': 64*1024*1024,
		'32 MB': 32*1024*1024,
		'16 MB': 16*1024*1024,
		'8 MB': 8*1024*1024,
		'4 MB': 4*1024*1024,
		'2 MB': 2*1024*1024,
		'1 MB': 1*1024*1024,
		'512 KB': 512*1024,
		'256 KB': 256*1024
	});
	f2.open();
	var f1 = gui.addFolder('Rendering');
	f1.add(renderSettings, 'maxPoints', {
		'20m': 20000000,
		'10m': 10000000,
		'5m': 5000000,
		'3m': 3000000,
		'2m': 2000000,
		'1m': 1000000,
		'500k': 500000
	});
	f1.add(renderSettings, 'colorRamp', Object.keys(ColorRamp.presets));
	f1.add(renderSettings, 'invertRamp');
	f1.add(renderSettings, 'pointSize').min(1).max(20);
	f1.add(renderSettings, 'showBounds');
	f1.open();
	var f3 = gui.addFolder('Actions');
	f3.add(actionMethods, 'update');
	f3.open();
	
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		
		if (!worker) {
			worker = new Worker('js/Worker-FileReader.js');
			worker.addEventListener('message', function(e) {
				if (e.data.header) {
					header = e.data.header.readObject("LASHeader");
					chunks = e.data.chunks;
					
					if (e.data.zstats && e.data.zstats.byteLength > 0) {
						statsZ = e.data.zstats.readObject("Statistics");
					}
					
					var displayOptionText = [
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
					].join('\n');
					
					$('#header-text').text(displayOptionText);

					var maxPoints = renderSettings.maxPoints;
					var points = header.numberOfPointRecords;
					pointStep = 1;
					if (points > maxPoints) {
						pointStep = Math.ceil(points / maxPoints);
						points = maxPoints;
						//console.log(String.format("thinning {0} to {1} (step {2})", header.numberOfPointRecords.toLocaleString(), points.toLocaleString(), pointStep));
					}

					if (renderSettings.showBounds) {
						var bounds = createBounds(header.extent);
						viewport.add(bounds);
					}
					
					if (resetCamera) {
						var es = header.extent.size();
						viewport.camera.position.z = Math.max(es.x, es.y) * 2;
					}
				}
				else if (e.data.chunk) {
					e.data.header = header;
					e.data.reader = new BinaryReader(e.data.chunk, 0, true);
					handleData(e.data);
					//console.log(String.format("chunk {0}", e.data.index));
					
					var progress = (100 * (e.data.index + 1) / chunks);
					$('#status-text').text(String.format('{0}%', ~~progress));
					
					if (e.data.index + 1 === chunks) {
						var timeSpan = Date.now() - startTime;
						//console.log(String.format("loaded in {0} ms", timeSpan.toLocaleString()));
						var bps = (header.numberOfPointRecords * header.pointDataRecordLength) / timeSpan * 1000;
						$('#status-text').text(String.format('({0} chunks in {1} ms @ {2}ps) 100%', chunks, timeSpan.toLocaleString(), bytesToSize(bps)));
					}
				}
			}, false);
		}
		
		if (header) {
			viewport.clearScene();
			header = null;
			statsZ = null;
			$('#header-text').text('');
			$('#status-text').text('');
		}
		
		startTime = Date.now();
		
		resetCamera = (file !== e.target.files[0]);
		file = e.target.files[0];
		worker.postMessage({file: file, chunkSize: loaderSettings.chunkSize});
	});
}

function handleData(data) {
	var object = createChunk(data);
	viewport.add(object);
}

function createChunk(data) {

	var points = ~~(data.points / pointStep);

	var material = new THREE.ParticleSystemMaterial({vertexColors: true, size: renderSettings.pointSize});
	//var material = shaderMaterial;
	
	var geometry = new THREE.BufferGeometry();

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets[renderSettings.colorRamp];
	if (renderSettings.invertRamp) {
		ramp = ramp.reverse();
	}
	
	var size = data.header.extent.size();
	var min = data.header.extent.min;
	var mid = data.header.extent.size().divideScalar(2).add(min);
	
	var stretch;
	if (statsZ) {
		stretch = new StdDevStretch(min.z, data.header.extent.max.z, statsZ, 2);
		mid.z = statsZ.modeApproximate;
	}
	else {
		stretch = new MinMaxStretch(min.z, data.header.extent.max.z);
	}
	var cachedRamp = new CachedColorRamp(ramp, stretch, 1000);
	
	var i = 0;
	for (var j = 0; j < data.points; j += pointStep, ++i) {
		data.reader.seek(j * data.pointSize);
		var point = data.reader.readUnquantizedPoint3D(data.header.quantization);
		
		var x = point.x;
		var y = point.y;
		var z = point.z;
		
		//var c = ramp.getColor((z - min.z) / size.z);
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
	
	if (statsZ) {
		var parent = new THREE.Object3D();
		parent.add(cube);
		var mid = extent.size().divideScalar(2).add(extent.min);
		parent.position.z -= (statsZ.modeApproximate - mid.z);
		cube = parent;
	}
	
	return cube;
}

init();