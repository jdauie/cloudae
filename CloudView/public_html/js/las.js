var LASVersion = {
	LAS_1_0: (1 << 8) | 0,
	LAS_1_1: (1 << 8) | 1,
	LAS_1_2: (1 << 8) | 2,
	LAS_1_3: (1 << 8) | 3,
	LAS_1_4: (1 << 8) | 4
};

function createZeroArray(length) {
    var a, i;
    a = new Array(length);
    for (i = 0; i < length; ++i) {
        a[i] = 0;
    }
    return a;
}

function LASProjectID(reader) {
    this.data = reader.readArrayBuffer(16);
}

function LASVersionInfo(reader) {
    this.versionMajor = reader.readUint8();
    this.versionMinor = reader.readUint8();
	this.version = (this.versionMajor << 8) | this.versionMinor;
}

function LASGlobalEncoding(reader) {
    this.globalEncoding = reader.readUint16();
}

function SQuantization3D(reader) {
	this.scale = reader.readVector3();
	this.offset = reader.readVector3();
}

BinaryReader.prototype.readBox3FromLAS = function() {
	var maxX = this.readFloat64();
	var minX = this.readFloat64();
	var maxY = this.readFloat64();
	var minY = this.readFloat64();
	var maxZ = this.readFloat64();
	var minZ = this.readFloat64();
	var min = new THREE.Vector3(minX, minY, minZ);
	var max = new THREE.Vector3(maxX, maxY, maxZ);
	return new THREE.Box3(min, max);
};

BinaryReader.prototype.readUnquantizedPoint3D = function(quantization) {
	return new THREE.Vector3(
		this.readInt32() * quantization.scale.x + quantization.offset.x,
		this.readInt32() * quantization.scale.y + quantization.offset.y,
		this.readInt32() * quantization.scale.z + quantization.offset.z
	);
};

function LASHeader(reader) {
	this.signature             = reader.readAsciiString(4);
	
    this.fileSourceID          = reader.readUint16();

	this.globalEncoding        = reader.readObject("LASGlobalEncoding");
	this.projectID             = reader.readObject("LASProjectID");
	this.version               = reader.readObject("LASVersionInfo");

	this.systemIdentifier      = reader.readAsciiString(32);
	this.generatingSoftware    = reader.readAsciiString(32);
	this.fileCreationDayOfYear = reader.readUint16();
	this.fileCreationYear      = reader.readUint16();

	this.headerSize            = reader.readUint16();
	this.offsetToPointData     = reader.readUint32();

	this.numberOfVariableLengthRecords = reader.readUint32();
	this.pointDataRecordFormat         = reader.readUint8();
	this.pointDataRecordLength         = reader.readUint16();
	this.legacyNumberOfPointRecords    = reader.readUint32();
	this.legacyNumberOfPointsByReturn  = reader.readUint32Array(5);

	this.quantization = reader.readObject("SQuantization3D");
	this.extent       = reader.readBox3FromLAS();

	if (this.version.version >= LASVersion.LAS_1_3)
	{
		this.startOfWaveformDataPacketRecord = reader.readUint64();
	}

	if (this.version.version >= LASVersion.LAS_1_4)
	{
		this.startOfFirstExtendedVariableLengthRecord = reader.readUint64();
		this.numberOfExtendedVariableLengthRecords    = reader.readUint32();
		this.numberOfPointRecords                     = reader.readUint64();
		this.numberOfPointsByReturn                   = reader.readUint64Array(15);
	}
	else
	{
		this.numberOfPointRecords = this.legacyNumberOfPointRecords;
		this.numberOfPointsByReturn = createZeroArray(15);
		for (var i = 0; i < this.legacyNumberOfPointsByReturn.Length; i++)
			this.numberOfPointsByReturn[i] = this.legacyNumberOfPointsByReturn[i];
	}
}