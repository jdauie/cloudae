#include "stdafx.h"

#include <msclr/marshal_cppstd.h>

#include "CloudAE.Interop.LASzip.h"

using namespace CloudAE::Interop::LAZ;

// the decoder can be initialized with point format/size instead of vlr
// I don't know whether that works in general
LAZInterop::LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr) {

	msclr::interop::marshal_context context;
	const char* pathStr = context.marshal_as<const char*>(path);
	m_file = fopen(pathStr, "r");
	if (m_file == NULL)
		throw gcnew System::Exception();

	// fseek returns 0 if successful
	if (fseek(m_file, dataOffset, SEEK_SET))
		throw gcnew System::Exception("Unable to seek");

	m_zip = new LASzip();

	cli::pin_ptr<unsigned char> pVLR = &vlr[0];
	if (!m_zip->unpack(pVLR, vlr->Length))
		throw gcnew System::Exception("Unable to unpack() LAZ VLR");
	
	m_unzipper = new LASunzipper();
	if (!m_unzipper->open(m_file, m_zip))
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


	//for (int i = 0; i < 1000; i++)
	//{
	//	ok = unzipper->read(m_lz_point);

	//	for (int j = 0; j < m_lz_point_size; j++)
	//	{
	//		//m_lz_point_data[j];
	//	}
	//}
}

LAZInterop::~LAZInterop() {
	
	m_unzipper->close();

	fclose(m_file);

	delete[] m_lz_point;
	delete[] m_lz_point_data;
}
