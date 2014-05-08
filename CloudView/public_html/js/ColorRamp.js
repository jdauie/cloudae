

function ColorRamp(name, colors) {
    this.name = name;
    this.colors = [];
	for (var i = 0; i < colors.length; i++) {
		this.colors.push(new THREE.Color(colors[i]));
	}
}

ColorRamp.prototype.reverse = function() {
	return new ColorRamp(this.name, this.colors.slice(0).reverse());
};
 
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
	]),
	AerialPerspective: new ColorRamp("Aerial Perspective", [
		0xFFFFFF,
		0x4F4F4F
	]),
	FilteredHillshade: new ColorRamp("Filtered Hillshade", [
		0xFFFFFF,
		0x545454
	]),
	DEM: new ColorRamp("DEM", [
		0xFFFCFC,
		0xF2DCD0
	]),
	// http://colorbrewer2.org/
	SequentialMultiHue1: new ColorRamp("Sequential Multi-hue 1", [
		0xf7fcfd,
		0xe5f5f9,
		0xccece6,
		0x99d8c9,
		0x66c2a4,
		0x41ae76,
		0x238b45,
		0x006d2c,
		0x00441b
	]),
	SequentialSingleHue1: new ColorRamp("Sequential Single-hue 1", [
		0xf7fbff,
		0xdeebf7,
		0xc6dbef,
		0x9ecae1,
		0x6baed6,
		0x4292c6,
		0x2171b5,
		0x08519c,
		0x08306b
	])
};