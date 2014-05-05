
var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: {
		fov:  45,
		near: 1,
		far:  200000
	}
});

var worker;
var file;
var header;
var chunks;
var pointStep;
var startTime;
var resetCamera;

var settings = {
	maxPoints: 1000000,
	colorRamp: 'Elevation1',
	pointSize: 1,
	render: onUpdateSettings
};

function onUpdateSettings() {
	var fileInput = $('#file-input')[0];
	fileInput.dispatchEvent(new Event('change'));
}

function init() {
	
	var gui = new dat.GUI();
	var f1 = gui.addFolder('Rendering');
	f1.add(settings, 'maxPoints', {
		'10m': 10000000,
		'5m': 5000000,
		'3m': 3000000,
		'2m': 2000000,
		'1m': 1000000,
		'500k': 500000
	});
	f1.add(settings, 'colorRamp', Object.keys(ColorRamp.presets));
	f1.add(settings, 'pointSize').min(1).max(20);
	f1.add(settings, 'render');
	f1.open();
	
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		if (!worker) {
			worker = new Worker('js/Worker-FileReader.js');
			worker.addEventListener('message', function(e) {
				if (e.data.header) {
					header = e.data.header.readObject("LASHeader");
					chunks = e.data.chunks;

					var maxPoints = settings.maxPoints;
					var points = header.numberOfPointRecords;
					pointStep = 1;
					if (points > maxPoints) {
						pointStep = Math.ceil(points / maxPoints);
						points = maxPoints;
						console.log(String.format("thinning {0} to {1} (step {2})", header.numberOfPointRecords.toLocaleString(), points.toLocaleString(), pointStep));
					}

					var bounds = createBounds(header.extent);
					viewport.add(bounds);

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
					//var progress = (100 * (e.data.index + 1) / chunks);
					
					if (e.data.index + 1 === chunks) {
						var timeSpan = Date.now() - startTime;
						console.log(String.format("loaded in {0} ms", timeSpan.toLocaleString()));
					}
				}
			}, false);
		}
		
		if (header) {
			viewport.clearScene();
		}
		
		startTime = Date.now();
		
		resetCamera = (file !== e.target.files[0]);
		file = e.target.files[0];
		worker.postMessage({file: file});
	});
}

function handleData(data) {
	var object = createChunk(data);
	viewport.add(object);
}

function createChunk(data) {

	var points = ~~(data.points / pointStep);

	var material = new THREE.ParticleSystemMaterial({vertexColors: true, size: settings.pointSize});
	//var material = shaderMaterial;
	
	var geometry = new THREE.BufferGeometry();

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets[settings.colorRamp];
	
	var size = data.header.extent.size();
	var min = data.header.extent.min;
	var mid = data.header.extent.size().divideScalar(2).add(min);
	
	var i = 0;
	for (var j = 0; j < data.points; j += pointStep, ++i) {
		data.reader.seek(j * data.pointSize);
		var point = data.reader.readUnquantizedPoint3D(data.header.quantization);
		
		var x = point.x;
		var y = point.y;
		var z = point.z;
		
		var c = ramp.getColor((z - min.z) / size.z);

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
	
	return cube;
}

init();