#include "stdafx.h"

#include <msclr/marshal_cppstd.h>

#include "CloudAE.Interop.LASzip.h"
#include "LAZBlockReader.h"

#include <iostream>
#include <fstream>

using namespace CloudAE::Interop::LAZ;

// the decoder can be initialized with point format/size instead of vlr
// I don't know whether that works in general
LAZInterop::LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr) {

	msclr::interop::marshal_context context;
	const char* pathStr = context.marshal_as<const char*>(path);
	
	m_stream = new ifstream(pathStr, ios::in | ios::binary);

	if (!m_stream->is_open())
		throw gcnew System::Exception("Unable to open");

	m_pointDataOffset = dataOffset;

	m_stream->seekg(dataOffset, ios::beg);

	m_zip = new LASzip();

	cli::pin_ptr<unsigned char> pVLR = &vlr[0];
	if (!m_zip->unpack(pVLR, vlr->Length))
		throw gcnew System::Exception("Unable to unpack() LAZ VLR");
	
	m_unzipper = new LASunzipper();
	if (!m_unzipper->open(*m_stream, m_zip))
		throw gcnew System::Exception("Unable to open() unzipper");

	// compute the point size
	m_lz_point_size = 0;
	for (unsigned int i = 0; i < m_zip->num_items; i++)
		m_lz_point_size += m_zip->items[i].size;

	// create the point data
	unsigned int point_offset = 0;
	m_lz_point = new unsigned char*[m_zip->num_items];
    
	m_lz_point_data = new unsigned char[m_lz_point_size];
	for (unsigned i = 0; i < m_zip->num_items; i++)
	{
		m_lz_point[i] = &(m_lz_point_data[point_offset]);
		point_offset += m_zip->items[i].size;
	}

	m_pointIndex = 0;

	m_blockReader = new LAZBlockReader(m_unzipper, m_lz_point, m_lz_point_data, m_lz_point_size);
}

void LAZInterop::Seek(long long byteOffset) {
	
	long long byteOffsetIntoPointData = (byteOffset - m_pointDataOffset);
	long long pointOffset = (byteOffsetIntoPointData / m_lz_point_size);

	if (pointOffset != m_pointIndex) {
		m_unzipper->seek(pointOffset);
		m_pointIndex = pointOffset;
	}
}

int LAZInterop::Read(array<Byte>^ buffer, int byteOffset, int byteCount) {
	
	cli::pin_ptr<unsigned char> pBuffer = &buffer[0];

	int bytesRead = m_blockReader->Read(pBuffer, byteOffset, byteCount);
	m_pointIndex += (bytesRead / m_lz_point_size);

	return bytesRead;
}

long long LAZInterop::GetPosition() {
	
	return (m_pointIndex * m_lz_point_size + m_pointDataOffset);
}

LAZInterop::~LAZInterop() {
	
	delete m_blockReader;

	m_unzipper->close();
	delete m_unzipper;

	m_stream->close();
	delete m_stream;

	delete[] m_lz_point;
	delete[] m_lz_point_data;
}
