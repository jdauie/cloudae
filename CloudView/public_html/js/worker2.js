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
