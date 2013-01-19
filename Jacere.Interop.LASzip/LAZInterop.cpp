#include <msclr/marshal_cppstd.h>

#include "LAZInterop.h"
#include "LAZBlockReader.h"

using namespace CloudAE::Interop::LAZ;

LAZInterop::LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr) {

	msclr::interop::marshal_context context;
	const char* pathStr = context.marshal_as<const char*>(path);

	cli::pin_ptr<unsigned char> pVLR = &vlr[0];
	
	m_blockReader = new LAZBlockReader(pathStr, dataOffset, pVLR, vlr->Length);
}

void LAZInterop::Seek(long long byteOffset) {
	
	m_blockReader->Seek(byteOffset);
}

int LAZInterop::Read(array<Byte>^ buffer, int byteOffset, int byteCount) {
	
	cli::pin_ptr<unsigned char> pBuffer = &buffer[0];
	int bytesRead = m_blockReader->Read(pBuffer, byteOffset, byteCount);
	return bytesRead;
}

long long LAZInterop::GetPosition() {
	
	return m_blockReader->GetPosition();
}

LAZInterop::~LAZInterop() {
	
	if (m_blockReader) {
		delete m_blockReader;
		m_blockReader = NULL;
	}
}
