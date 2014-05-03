
var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: {
		fov:  75,
		near: 1,
		far:  200000
	}
});

var worker;
var header;

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
			else if (e.data.header) {
				var br = new BinaryReader(e.data.header, 0, true);
				header = br.readObject("LASHeader");
				console.log("header");
				//console.log(String.format(" extent x: {0}"), header.extent.size().x);
			}
			else if (e.data.chunk) {
				e.data.header = header;
				e.data.reader = new BinaryReader(e.data.chunk, 0, true);
				handleData(e.data);
				console.log(String.format("chunk {0}", e.data.index));
			}
			else {
				console.log("else");
			}
		}, false);
		worker.postMessage({file: file});
	});
}

function handleData(data) {
	//var file = new LASFile(arrayBuffer);
	$(".ajax-content").hide();
	var object = createGeometry(data);
	viewport.add(object);
}

function createGeometry(data) {

	var maxPoints = 10000;
	var points = data.points;
	var pointStep = 1;
	if (points > maxPoints) {
		pointStep = Math.ceil(points / maxPoints);
		points = maxPoints;
	}

	var geometry = new THREE.BufferGeometry();
	geometry.dynamic = false;

	var material = new THREE.ParticleSystemMaterial({ vertexColors: true });

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	var positions = geometry.attributes.position.array;
	var colors = geometry.attributes.color.array;

	var ramp = ColorRamp.presets.Elevation1;
	
	var es = data.header.extent.size();
	var em = data.header.extent.min;
	
	var ec = (es.x > es.y) ? es.x : es.y;
	
	var i = 0;
	for (var j = 0; j < data.points; j += pointStep) {
		i = j / pointStep;
		data.reader.seek(j * data.pointSize);
		var point = data.reader.readUnquantizedPoint3D(data.header.quantization);
		
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

	geometry.computeBoundingSphere();
	var mesh = new THREE.ParticleSystem(geometry, material);
	
	return mesh;
}

loadData();