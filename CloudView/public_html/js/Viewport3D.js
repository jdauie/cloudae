
function Viewport3D(container, settings) {
	
	this.container = container;
	
	var HEIGHT = window.innerHeight;
	var WIDTH  = window.innerWidth;

	this.renderer = new THREE.WebGLRenderer();
	this.renderer.setSize(WIDTH, HEIGHT);
	this.container.appendChild(this.renderer.domElement);

	this.stats = new Stats();
	this.stats.domElement.style.position = 'absolute';
	this.stats.domElement.style.top = '0px';
	this.container.appendChild(this.stats.domElement);

	this.camera = new THREE.PerspectiveCamera(settings.camera.fov, WIDTH / HEIGHT, settings.camera.near, settings.camera.far);
	//this.camera.position.z = 1000;
	
	this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
	
	/*this.controls = new THREE.TrackballControls(this.camera);
	
	this.controls.rotateSpeed = 1.0;
	this.controls.zoomSpeed = 1.2;
	this.controls.panSpeed = 0.8;

	this.controls.noZoom = false;
	this.controls.noPan = false;

	this.controls.staticMoving = true;
	this.controls.dynamicDampingFactor = 0.3;

	this.controls.keys = [65, 83, 68];*/
	
	
	this.scene = new THREE.Scene();
	
	
	this.render = function() {
		this.renderer.render(this.scene, this.camera);
	};

	this.update = function() {
		this.controls.update();
		this.stats.update();
	};
	
	this.onWindowResize = function() {
		var HEIGHT = window.innerHeight;
		var WIDTH  = window.innerWidth;

		this.renderer.setSize(WIDTH, HEIGHT);

		this.camera.aspect = (WIDTH / HEIGHT);
		this.camera.updateProjectionMatrix();

		//this.controls.handleResize();
	};
	
	this.add = function(object) {
		this.scene.add(object);
	};
	
	this.clearScene = function() {
		while (this.scene.children.length > 0) {
			this.scene.remove(this.scene.children[this.scene.children.length - 1]);
		}
	};
	
	this.controls.addEventListener('change', Viewport3D.render);
	
	window.addEventListener('resize', Viewport3D.onWindowResize, false);
};

Viewport3D.create = function(container, settings) {
	if (!Viewport3D.viewports) {
		Viewport3D.viewports = [];
		
		document.addEventListener("visibilitychange", Viewport3D.handleVisibilityChange, false);
	}
	
	var viewport = new Viewport3D(container, settings);
	Viewport3D.viewports.push(viewport);
	
	// todo: make sure only one animation loop runs
	
	Viewport3D.animate();
	return viewport;
};

Viewport3D.animate = function() {
	if (Viewport3D.paused)
		return;

	requestAnimationFrame(Viewport3D.animate);
	for(var i = 0; i < Viewport3D.viewports.length; i++) {
		var viewport = Viewport3D.viewports[i];
		viewport.render();
		viewport.update();
	}
};

Viewport3D.render = function() {
	for(var i = 0; i < Viewport3D.viewports.length; i++) {
		var viewport = Viewport3D.viewports[i];
		viewport.render();
	}
};

Viewport3D.onWindowResize = function() {
	for(var i = 0; i < Viewport3D.viewports.length; i++) {
		var viewport = Viewport3D.viewports[i];
		viewport.onWindowResize();
	}
	Viewport3D.render();
};

Viewport3D.handleVisibilityChange = function() {
	if (document["hidden"]) {
		Viewport3D.paused = true;
		console.log("paused");
	} else {
		Viewport3D.paused = false;
		Viewport3D.animate();
		console.log("resumed");
	}
};
