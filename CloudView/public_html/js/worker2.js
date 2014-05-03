self.addEventListener('message', function(e) {
  var data = e.data;
  loadFile(data.file);
}, false);

function loadFile(file) {
	var reader = new FileReaderSync();
	//reader.onprogress = updateProgress;
	
	var arraybuffer = reader.readAsArrayBuffer(file);
	self.postMessage(arraybuffer, [arraybuffer]);
}

function updateProgress(evt) {
	if (evt.lengthComputable) {
		var percentComplete = (evt.loaded / evt.total) * 100;
		self.postMessage({progress: ~~percentComplete});
	}
}

/*
function sliceMe() {
	var file = document.getElementById('file').files[0],
		fr = new FileReader,
		chunkSize = 2097152,
		chunks = Math.ceil(file.size / chunkSize),
		chunk = 0;

	function loadNext() {
		var start, end,
				blobSlice = File.prototype.mozSlice || File.prototype.webkitSlice;

		start = chunk * chunkSize;
		end = start + chunkSize >= file.size ? file.size : start + chunkSize;

		fr.onload = function() {
			if (++chunk < chunks) {
				//console.info(chunk);
				loadNext(); // shortcut here
			}
		};
		fr.readAsBinaryString(blobSlice.call(file, start, end));
	}

	loadNext();
}
*/
