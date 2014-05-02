var camera, scene, renderer;
var positions, colors;
var points;
var geometry;
var mesh;
var stats;

var controls;

var file;

var worker;

if (!String.format) {
  String.format = function(format) {
    var args = Array.prototype.slice.call(arguments, 1);
    return format.replace(/{(\d+)}/g, function(match, number) { 
      return typeof args[number] !== 'undefined'
        ? args[number] 
        : match
      ;
    });
  };
  String.prototype.format = String.format;
}

function LASFile(arrayBuffer) {
	var reader = new BinaryReader(arrayBuffer, 0, true);
	this.header = reader.readObject("LASHeader");
	this.reader = new BinaryReader(arrayBuffer, this.header.offsetToPointData, true);
}

function loadData3() {
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		var reader = new FileReader();
		var file = fileInput.files[0];
		reader.readAsArrayBuffer(file);
		
		reader.onprogress = function(evt) {
			if (evt.lengthComputable) {
				var percentComplete = (evt.loaded / evt.total) * 100;
				$('#ajax-progress').val(percentComplete);
			}
		};
		
		reader.onload = function() {
			handleData(reader.result);
		};
	});
};

function loadData() {
	var fileInput = $('#file-input')[0];

	fileInput.addEventListener('change', function(e) {
		var file = fileInput.files[0];
		
		worker = new Worker('/3d/public_html/js/worker2.js');
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

function loadData2() {
	//var url = "/3d/public_html/TO_core_last.las";
	var url = "/3d/public_html/points_a1_Kabul_tile15a_step3_low.las";
	
	worker = new Worker('/3d/public_html/js/worker.js');
	worker.addEventListener('message', function(e) {
		if (e.data.progress) {
			$('#ajax-progress').val(e.data.progress);
		}
		else {
			handleData(e.data);
		}
	}, false);
	worker.postMessage({url: url});
}

function handleData(arrayBuffer) {
	file = new LASFile(arrayBuffer);
	
	//$(".ajax-progress").text("Converting...");
	
	$(".ajax-content").hide();
	init();
	animate();
}

function loadURL(url) {
	var xhr = new XMLHttpRequest();
	xhr.open('GET', url, true);
	xhr.responseType = 'arraybuffer';

	xhr.onload = function(e) {
		handleData(this.response);
	};

	xhr.send();
}

function createGeometry() {

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

function add_points_buffer_geom() {

	points = file.header.numberOfPointRecords;

	geometry = new THREE.BufferGeometry();
	geometry.dynamic = false;

	var material = new THREE.ParticleSystemMaterial({ vertexColors: true });

	geometry.addAttribute('position', Float32Array, points, 3);
	geometry.addAttribute('color', Float32Array, points, 3);

	positions = geometry.attributes.position.array;
	colors = geometry.attributes.color.array;

	createGeometry();

	geometry.computeBoundingSphere();
	mesh = new THREE.ParticleSystem(geometry, material);
	scene.add(mesh);
}

function init() {
	var $container = $('#container');

	renderer = new THREE.WebGLRenderer();
	renderer.setSize(window.innerWidth, window.innerHeight);
	container.appendChild(renderer.domElement);

	stats = new Stats();
	stats.domElement.style.position = 'absolute';
	stats.domElement.style.top = '0px';
	container.appendChild(stats.domElement);

	camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 1, 200000);
	camera.position.z = 500;
	
	controls = new THREE.OrbitControls(camera, renderer.domElement);
	
	/*controls = new THREE.TrackballControls(camera);
	
	controls.rotateSpeed = 1.0;
	controls.zoomSpeed = 1.2;
	controls.panSpeed = 0.8;

	controls.noZoom = false;
	controls.noPan = false;

	controls.staticMoving = true;
	controls.dynamicDampingFactor = 0.3;

	controls.keys = [65, 83, 68];
	*/
	controls.addEventListener('change', render);
	

	/*
	controls = new THREE.FlyControls(camera);

	controls.movementSpeed = 1000;
	controls.domElement = container;
	controls.rollSpeed = Math.PI / 24;
	controls.autoForward = false;
	controls.dragToLook = false;
	*/

	scene = new THREE.Scene();

	add_points_buffer_geom();

	window.addEventListener('resize', onWindowResize, false);
}

/*function animate() {

	//mesh.rotation.x += 0.01;
	//mesh.rotation.y += 0.01;
	mesh.rotation.z += 0.01;

	//requestAnimationFrame(animate);

	renderer.render(scene, camera);

	stats.update();
}*/

function onWindowResize() {
	SCREEN_HEIGHT = window.innerHeight;
	SCREEN_WIDTH  = window.innerWidth;

	renderer.setSize( SCREEN_WIDTH, SCREEN_HEIGHT );

	camera.aspect = SCREEN_WIDTH / SCREEN_HEIGHT;
	camera.updateProjectionMatrix();

	controls.handleResize();

	render();
}

function animate() {
	requestAnimationFrame(animate);
	render();
	update();
}

function update() {
	controls.update();
	stats.update();
}

function render() {
	/*
	var delta = clock.getDelta();
	meshPlanet.rotation.y += rotationSpeed * delta;
	meshClouds.rotation.y += 1.25 * rotationSpeed * delta;

	// slow down as we approach the surface
	dPlanet = camera.position.length();
	dMoonVec.subVectors( camera.position, meshMoon.position );
	dMoon = dMoonVec.length();

	if ( dMoon < dPlanet ) {
		d = ( dMoon - radius * moonScale * 1.01 );
	}
	else {
		d = ( dPlanet - radius * 1.01 );
	}

	controls.movementSpeed = 0.33 * d;
	controls.update( delta );
	*/
	
	renderer.render(scene, camera);
}

loadData();