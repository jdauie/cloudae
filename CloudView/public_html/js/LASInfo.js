
(function(JACERE) {
	
	JACERE.LASInfo = function(file) {
		this.file = file;
		this.texture = null;
		this.tileIndex = 0;
		this.fileSize = 0;
		this.time = [Date.now()];
		
		if (file.constructor.name === "File") {
			this.name = file.name;
		}
		else {
			this.name = file.replace(/^.*[\\\/]/, '');
		}

		this.settings = {
			loader: JACERE.Util.clone(settings.loader),
			render: JACERE.Util.clone(settings.render)
		};

		this.geometry = {
			bounds: null,
			progress: null,
			chunks: []
		};
		
	};
	
	JACERE.LASInfo.prototype = {
		
		constructor: JACERE.LASInfo,
		
		setHeader: function(header, length, tiles, stats) {
			this.header = header;
			this.tiles = tiles;
			this.stats = stats;
			this.fileSize = length;
			//this.step = Math.ceil(header.numberOfPointRecords / Math.min(this.settings.loader.maxPoints, header.numberOfPointRecords));
			this.step = (header.numberOfPointRecords / Math.min(this.settings.loader.maxPoints, header.numberOfPointRecords));
			this.radius = header.extent.size().length() / 2;
			
			this.time.push(Date.now());

			this.update();
		},
		
		setTime: function() {
			this.time.push(Date.now());
		},

		getHeaderTime: function() {
			return (this.time[1] - this.time[0]);
		},

		getLoadTime: function() {
			return (this.time[2] - this.time[1]);
		},

		getPointReader: function(buffer, pointIndex, pointCount) {
			return new JACERE.PointReader(this, buffer, pointIndex, pointCount);
		},
		
		settingsChanged: function(s1, s2, keys) {
			for (var i = 0; i < keys.length; i++) {
				if (s1[keys[i]] !== s2[keys[i]])
					return true;
			}
			return false;
		},

		updateRenderSettings: function(silent) {
			
			// I should remove 'showBounds' from the things that matter for updating
			var changed = this.settingsChanged(this.settings.render, settings.render, Object.keys(settings.render));
			
			this.settings.render = JACERE.Util.clone(settings.render);
			
			if (!silent && changed) {
				this.update();
			}
		},

		update: function() {
			this.updateColorMap();
			this.updateTexture();
			this.updateMaterial();

			// update attributes if necessary
			if (this.material) {// && this.material.attributes) {
				this.forceColorUpdate();
			}
		},

		forceColorUpdate: function() {
			if (!this.material || this.geometry.chunks.length === 0)// || !this.geometry.chunks[0].geometry.attributes.color)
				return;

			for (var i = 0; i < this.geometry.chunks.length; i++) {
				var obj = this.geometry.chunks[i];
				updateChunkPackedColor(obj);
			}
		},

		updateMaterial: function() {
			if (this.settings.render.colorMode === 'rgb' && JACERE.PointReader.hasColor(this.header.pointDataRecordFormat)) {
				//if (!current.material) {
					var uniforms = {
						size: { type: "f", value: this.settings.render.pointSize }
					};

					var attributes = {
						color: {type: 'f', value: null}
					};

					this.material = new THREE.ShaderMaterial({
						uniforms:       uniforms,
						attributes:     attributes,
						vertexShader:   settings.shaders['packed.vert'].shader,
						fragmentShader: settings.shaders['color.frag'].shader
					});
				//}
			}
			else {
				//if (!current.material) {
					var uniforms = {
						size:     { type: "f", value: this.settings.render.pointSize },
						zmin:     { type: "f", value: this.header.extent.min.z },
						zscale:   { type: "f", value: 1 / this.header.extent.size().z },
						texture:  { type: "t", value: this.texture }
					};

					this.material = new THREE.ShaderMaterial( {
						uniforms: 		uniforms,
						vertexShader:   settings.shaders['texture.vert'].shader,
						fragmentShader: settings.shaders['color.frag'].shader
					});
				//}
			}
			
			// this is old code from when I didn't change between materials
			if (this.material) {
				if (!this.material.vertexColors) {
					this.material.uniforms.size.value = this.settings.render.pointSize;
				}
				else {
					this.material.size = this.settings.render.pointSize;
				}
				this.material.needsUpdate = true;
			}
		},

		updateColorMap: function() {
			var ramp = JACERE.ColorRamp.presets[this.settings.render.colorRamp];
			if (this.settings.render.invertRamp) {
				ramp = ramp.reverse();
			}

			var min = this.header.extent.min;
			var max = this.header.extent.max;

			var stretch = (this.settings.render.useStats && this.stats)
				? new JACERE.StdDevStretch(min.z, max.z, this.stats, 2)
				: new JACERE.MinMaxStretch(min.z, max.z);

			this.colorMap = new JACERE.CachedColorMap(ramp, stretch, this.settings.render.colorValues);
		},

		updateTexture: function() {
			if (!this.texture) {
				this.texture = new THREE.Texture(this.createGradient(this.colorMap));
				settings.elements.ramp.append(this.texture.image);
			}
			else {
				this.createGradient(this.colorMap, this.texture.image);
			}
			this.texture.needsUpdate = true;
		},

		createGradient: function(map, canvas) {
			var size = map.length;

			if (!canvas) {
				canvas = document.createElement('canvas');
				canvas.width = 1;
				canvas.height = size;
			}

			var context = canvas.getContext('2d');

			context.rect(0, 0, 1, size);
			var gradient = context.createLinearGradient(0, size, 0, 0);

			//var stops = map.ramp.colors.length;
			var stops = map.length;
			for (var i = 0; i < stops; ++i) {
				var ratio = i / (stops - 1);
				//gradient.addColorStop(ratio, '#' + map.ramp.getColor(ratio).getHexString());
				gradient.addColorStop(ratio, '#' + map.bins[i].getHexString());
			}

			context.fillStyle = gradient;
			context.fill();

			return canvas;
		},
		
		dispose: function() {
			settings.elements.ramp.empty();
		}
	};
	
}(self.JACERE = self.JACERE || {}));