
(function(JACERE) {
	
	JACERE.Viewport3D = function(container, settings) {

		this.container = container;

		var HEIGHT = window.innerHeight;
		var WIDTH  = window.innerWidth;

		this.renderer = new THREE.WebGLRenderer({antialias: false});
		this.renderer.setSize(WIDTH, HEIGHT);
		this.renderer.autoClear = false;
		//this.renderer.setFaceCulling(0);
		this.container.appendChild(this.renderer.domElement);

		this.stats = new Stats();
		this.stats.domElement.style.position = 'absolute';
		this.stats.domElement.style.top = '0px';
		this.container.appendChild(this.stats.domElement);

		this.camera = new THREE.PerspectiveCamera(settings.camera.fov, WIDTH / HEIGHT, settings.camera.near, settings.camera.far);
		

		//this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);


		this.controls = new THREE.TrackballControls(this.camera, this.renderer.domElement);

		//this.controls.rotateSpeed = 1.0;
		//this.controls.zoomSpeed = 1.2;
		//this.controls.panSpeed = 0.8;

		//this.controls.noZoom = false;
		//this.controls.noPan = false;

		//this.controls.staticMoving = true;
		this.controls.dynamicDampingFactor = 0.5;

		this.controls.keys = [65, 83, 68];


		this.scene = new THREE.Scene();


		// double rendering
		// this should only happen if I am *NOT* doing automatic requestAnimationFrame()
		//this.controls.addEventListener('change', Viewport3D.render);

		window.addEventListener('resize', JACERE.Viewport3D.onWindowResize, false);
	};
	
	JACERE.Viewport3D.prototype = {
		
		constructor: JACERE.Viewport3D,
		
		render: function() {
			this.renderer.render(this.scene, this.camera);
		},

		update: function() {
			this.controls.update();
			this.stats.update();
		},

		onWindowResize: function() {
			var HEIGHT = window.innerHeight;
			var WIDTH  = window.innerWidth;

			this.renderer.setSize(WIDTH, HEIGHT);

			this.camera.aspect = (WIDTH / HEIGHT);
			this.camera.updateProjectionMatrix();

			//this.controls.handleResize();
		},

		add: function(object) {
			this.scene.add(object);
		},

		remove: function(object) {
			this.scene.remove(object);
		},

		clearScene: function() {
			while (this.scene.children.length > 0) {
				this.scene.remove(this.scene.children[this.scene.children.length - 1]);
			}
		}
	};

	JACERE.Viewport3D.create = function(container, settings) {
		if (JACERE.Viewport3D.viewport)
			throw "viewport already created";

		document.addEventListener("visibilitychange", JACERE.Viewport3D.handleVisibilityChange, false);

		this.viewport = new JACERE.Viewport3D(container, settings);

		JACERE.Viewport3D.animate();
		
		return this.viewport;
	};

	JACERE.Viewport3D.animate = function() {
		if (JACERE.Viewport3D.paused)
			return;

		requestAnimationFrame(JACERE.Viewport3D.animate);
		
		updateFrustrumRange();
		
		JACERE.Viewport3D.viewport.render();
		JACERE.Viewport3D.viewport.update();
	};

	JACERE.Viewport3D.onWindowResize = function() {
		JACERE.Viewport3D.viewport.onWindowResize();
	};

	JACERE.Viewport3D.handleVisibilityChange = function() {
		if (document["hidden"]) {
			JACERE.Viewport3D.paused = true;
			//console.log("paused");
		} else {
			JACERE.Viewport3D.paused = false;
			Viewport3D.animate();
			//console.log("resumed");
		}
	};
	
}(self.JACERE = self.JACERE || {}));