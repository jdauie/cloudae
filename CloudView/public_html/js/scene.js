
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

function init() {
	
	var UIText = function () {
		this['MaxPoints'] = 1000000;
	};

	var maxPoints = new UIText();

	var gui = new dat.GUI();
	var controller = gui.add(maxPoints, 'MaxPoints', {
		'10m points': 10000000,
		'5m points': 5000000,
		'3m points': 3000000,
		'2m points': 2000000,
		'1m points': 1000000,
		'500k points': 500000
	});
	controller.onChange(function(value) {

		//scene.remove(mesh);

		//mesh.geometry.dispose();

		//GenerateGeometry2();

	});

	// use
	//var text;
	//var cubes = text['Cube Count'];
	
	
	
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		if (!worker) {
			worker = new Worker('js/worker2.js');
			worker.addEventListener('message', function(e) {
				if (e.data.header) {
					header = e.data.header.readObject("LASHeader");
					chunks = e.data.chunks;

					var maxPoints = 1000000;
					var points = header.numberOfPointRecords;
					pointStep = 1;
					if (points > maxPoints) {
						pointStep = Math.ceil(points / maxPoints);
						points = maxPoints;
						console.log(String.format("thinning {0} to {1} (step {2})", header.numberOfPointRecords.toLocaleString(), points.toLocaleString(), pointStep));
					}

					var bounds = createBounds(header.extent);
					viewport.add(bounds);

					var es = header.extent.size();
					viewport.camera.position.z = Math.max(es.x, es.y) * 2;
				}
				else if (e.data.chunk) {
					e.data.header = header;
					e.data.reader = new BinaryReader(e.data.chunk, 0, true);
					handleData(e.data);
					//console.log(String.format("chunk {0}", e.data.index));
					//var progress = (100 * (e.data.index + 1) / chunks);
					
					if (e.data.index + 1 == chunks) {
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

	var geometry = new THREE.BufferGeometry();
	geometry.dynamic = false;

	var material = new THREE.ParticleSystemMaterial({ vertexColors: true });

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets.Elevation1;
	
	var size = data.header.extent.size();
	var min = data.header.extent.min;
	var mid = data.header.extent.size().divideScalar(2).add(min);
	
	var i = 0;
	for (var j = 0; j < data.points; j += pointStep) {
		++i;
		data.reader.seek(j * data.pointSize);
		var point = data.reader.readUnquantizedPoint3D(data.header.quantization);
		
		var x = point.x;
		var y = point.y;
		var z = point.z;
		
		var c = ramp.getColor((z - min.z) / size.z);

		x = (x - mid.x);
		y = (y - mid.y);
		z = (z - mid.z);

		positions[ i * 3 + 0 ] = x;
		positions[ i * 3 + 1 ] = y;
		positions[ i * 3 + 2 ] = z;

		colors[ i * 3 + 0 ] = c.r;
		colors[ i * 3 + 1 ] = c.g;
		colors[ i * 3 + 2 ] = c.b;
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