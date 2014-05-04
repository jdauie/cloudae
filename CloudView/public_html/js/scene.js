
var container = document.getElementById('container');
var viewport = Viewport3D.create(container, {
	camera: {
		fov:  45,
		near: 1,
		far:  200000
	}
});

var worker;
var header;
var pointStep;

function loadData() {
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		var file = fileInput.files[0];
		
		worker = new Worker('js/worker2.js');
		worker.addEventListener('message', function(e) {
			if (e.data.header) {
				header = e.data.header.readObject("LASHeader");
				console.log("header");
				
				var maxPoints = 1000000;
				var points = header.numberOfPointRecords;
				pointStep = 1;
				if (points > maxPoints) {
					pointStep = Math.ceil(points / maxPoints);
					points = maxPoints;
					console.log(String.format("thinning {0} to {1} (step {2})", header.numberOfPointRecords, points, pointStep));
				}
			}
			else if (e.data.chunk) {
				e.data.header = header;
				e.data.reader = new BinaryReader(e.data.chunk, 0, true);
				handleData(e.data);
				//console.log(String.format("chunk {0}", e.data.index));
			}
		}, false);
		worker.postMessage({file: file});
	});
}

function handleData(data) {
	$(".ajax-content").hide();
	
	//var object = createExtentGeometry(data.header.extent);
	//viewport.add(object);
	
	var object = createGeometry(data);
	viewport.add(object);
}

function createGeometry(data) {

	var points = ~~(data.points / pointStep);

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
		++i;
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
	
	return new THREE.ParticleSystem(geometry, material);
}

function createExtentGeometry(extent) {
	var segments = 10000;

	var geometry = new THREE.BufferGeometry();
	var material = new THREE.LineBasicMaterial({ vertexColors: true });

	geometry.addAttribute( 'position', new Float32Array( segments * 3 ), 3 );
	geometry.addAttribute( 'color', new Float32Array( segments * 3 ), 3 );

	var positions = geometry.getAttribute( 'position' ).array;
	var colors = geometry.getAttribute( 'color' ).array;

	var r = 800;

	for ( var i = 0; i < segments; i ++ ) {

		var x = Math.random() * r - r / 2;
		var y = Math.random() * r - r / 2;
		var z = Math.random() * r - r / 2;

		// positions

		positions[ i * 3 ] = x;
		positions[ i * 3 + 1 ] = y;
		positions[ i * 3 + 2 ] = z;

		// colors

		colors[ i * 3 ] = ( x / r ) + 0.5;
		colors[ i * 3 + 1 ] = ( y / r ) + 0.5;
		colors[ i * 3 + 2 ] = ( z / r ) + 0.5;

	}

	geometry.computeBoundingSphere();

	return new THREE.Line(geometry, material);
}

loadData();