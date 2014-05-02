

function ColorRamp(name, colors) {
    this.name = name;
    this.colors = [];
	for (var i = 0; i < colors.length; i++) {
		this.colors.push(new THREE.Color(colors[i]));
	}
}
 
ColorRamp.prototype.getColor = function(scaledValue) {
	//if (scaledValue < 0 || scaledValue > 1)
	//	throw new ArgumentException("scaledValue is outside valid range", "scaledValue");
	
	//if (scaledValue < 0 || scaledValue > 1)
	//	alert("color scaled out of bounds: "+scaledValue);
	
	if (scaledValue < 0) scaledValue = 0;
	else if (scaledValue > 1) scaledValue = 1;
	
	var mapScale = scaledValue * (this.colors.length - 1);
	var mapScaleMin = Math.floor(mapScale);
	var remainder = mapScale - mapScaleMin;
	
	var color = this.colors[mapScaleMin].clone();

	if (remainder > 0) {
		color = color.lerp(this.colors[mapScaleMin + 1], remainder);
	}

	return color;
};

ColorRamp.presets = {
	Grayscale: new ColorRamp("Grayscale", [
		0x000000,
		0xffffff
	]),
	Elevation1: new ColorRamp("Elevation 1", [
		0xAFF0E9,
		0xFFFFB3,
		0x008040,
		0xFCBA03,
		0x800000,
		0x69300D,
		0xABABAB,
		0xFFFCFF
	]),
	Elevation2: new ColorRamp("Elevation 2", [
		0x76DBD3,
		0xFFFFC7,
		0xFFFFC7,
		0xFFFF80,
		0xD9C279,
		0x876026,
		0x9696B5,
		0xB596B5,
		0x19FCFF
	]),
	BareEarth: new ColorRamp("Bare Earth", [
		0xFFFF80,
		0xF2A72E,
		0x6B0000
	]),
	FullSpectrum: new ColorRamp("Full Spectrum", [
		0xFF0000,
		0xFFFF00,
		0x00FFFF,
		0x0000FF
	]),
	PartialSpectrum: new ColorRamp("Partial Spectrum", [
		0x734D2A,
		0x9C6930,
		0xC98934,
		0xE8C174,
		0xFFFFBF,
		0xAD95BA,
		0x5B3FB0,
		0x592787,
		0x510D61
	])
};
