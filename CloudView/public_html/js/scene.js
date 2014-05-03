
var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: {
		fov:  75,
		near: 1,
		far:  200000
	}
});

var worker;

function LASFile(arrayBuffer) {
	var reader = new BinaryReader(arrayBuffer, 0, true);
	this.header = reader.readObject("LASHeader");
	this.reader = new BinaryReader(arrayBuffer, this.header.offsetToPointData, true);
}

function loadData() {
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		var file = fileInput.files[0];
		
		worker = new Worker('/cloudview/js/worker2.js');
		worker.addEventListener('message', function(e) {
			if (e.data.progress) {
				$('#ajax-progress').val(e.data.progress);
			}
			else {
				handleData(e.data);
			}
		}, false);
		worker.postMessage({file: file});
	});
}

function handleData(arrayBuffer) {
	var file = new LASFile(arrayBuffer);
	$(".ajax-content").hide();
	var object = createGeometry(file);
	viewport.add(object);
}

function createGeometry(file) {

	var points = file.header.numberOfPointRecords;

	var geometry = new THREE.BufferGeometry();
	geometry.dynamic = false;

	var material = new THREE.ParticleSystemMaterial({ vertexColors: true });

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	populateGeometry(file, geometry);

	geometry.computeBoundingSphere();
	var mesh = new THREE.ParticleSystem(geometry, material);
	
	return mesh;
}

function populateGeometry(file, geometry) {
	
	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets.Elevation1;
	
	var es = file.header.extent.size();
	var em = file.header.extent.min;
	
	var ec = (es.x > es.y) ? es.x : es.y;
	
	for (var i = 0; i < file.header.numberOfPointRecords; i++) {
		file.reader.seek(i * file.header.pointDataRecordLength);
		var point = file.reader.readUnquantizedPoint3D(file.header.quantization);
		
		var x = point.x;
		var y = point.y;
		var z = point.z;
		
		var c = ramp.getColor((z - em.z) / es.z);

		x = ((x - em.x) / ec) * 1000 - 500;
		y = ((y - em.y) / ec) * 1000 - 500;
		z = ((z - em.z) / ec) * 1000 - 500;

		positions[ i * 3 + 0 ] = x;
		positions[ i * 3 + 1 ] = y;
		positions[ i * 3 + 2 ] = z;

		colors[ i * 3 + 0 ] = c.r;
		colors[ i * 3 + 1 ] = c.g;
		colors[ i * 3 + 2 ] = c.b;
	}
}

loadData();