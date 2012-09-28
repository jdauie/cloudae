#include "LAZBlockReader.h"

#include <errno.h>

LAZBlockReader::LAZBlockReader(const char* path, unsigned long dataOffset, unsigned char* vlr, unsigned int vlrLength) {
	
	m_streamBuffer = NULL;
	m_stream = NULL;
	m_file = NULL;
	m_zip = NULL;
	m_unzipper = NULL;
	m_lz_point = NULL;
	m_lz_point_data = NULL;
	m_lz_point_size = NULL;


	m_pointDataOffset = dataOffset;

	int bufferSize = 1024 * 1024;
	//int bufferSize = 64 * 1024;

	//m_streamBuffer = new char[bufferSize];
	//m_stream = new ifstream(path, ios::in | ios::binary);
	////if (!m_stream->rdbuf()->pubsetbuf(m_streamBuffer, bufferSize))
	////	return;
	////m_stream->open(path, ios::in | ios::binary);

	//if (!m_stream->is_open())
	//	return;

	//m_stream->seekg(dataOffset, ios::beg);

	m_file = fopen(path, "rb");
	if (!m_file) {
		printf ("Error opening file: %s\n", strerror(errno));
		return;
	}

	m_streamBuffer = new char[bufferSize];
	setvbuf(m_file, m_streamBuffer, _IOFBF, bufferSize);

	if (fseek(m_file, dataOffset, SEEK_SET))
		return;


	m_zip = new LASzip();
	if (!m_zip->unpack(vlr, vlrLength))
		return;
	
	m_unzipper = new LASunzipper();
	//if (!m_unzipper->open(*m_stream, m_zip))
	//	return;
	if (!m_unzipper->open(m_file, m_zip))
		return;

	// compute the point size
	m_lz_point_size = 0;
	for (unsigned int i = 0; i < m_zip->num_items; i++)
		m_lz_point_size += m_zip->items[i].size;

	if (m_zip->num_items == 0)
		throw 101;

	if (m_lz_point_size == 0)
		throw 102;

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
}

void LAZBlockReader::Seek(long long byteOffset) {
	
	long long byteOffsetIntoPointData = (byteOffset - m_pointDataOffset);
	long long pointOffset = (byteOffsetIntoPointData / m_lz_point_size);

	if (pointOffset != m_pointIndex) {
		m_unzipper->seek(pointOffset);
		m_pointIndex = pointOffset;
	}
}

int LAZBlockReader::Read(unsigned char* buffer, int byteOffset, int byteCount) {
	
	unsigned char* bufferStart = buffer + byteOffset;
	unsigned char* bufferEnd = bufferStart + byteCount;
	unsigned char* bufferCurrent = bufferStart;

	while (bufferCurrent < bufferEnd)
	{
		if (!m_unzipper->read(m_lz_point))
			break;

		memcpy(bufferCurrent, m_lz_point_data, m_lz_point_size);
		bufferCurrent += m_lz_point_size;

		/*for (int j = 0; j < m_lz_point_size; j++)
		{
			*bufferCurrent = m_lz_point_data[j];
			++bufferCurrent;
		}*/
	}

	int bytesRead = (bufferCurrent - bufferStart);
	m_pointIndex += (bytesRead / m_lz_point_size);

	return bytesRead;
}

long long LAZBlockReader::GetPosition() {
	
	return (m_pointIndex * m_lz_point_size + m_pointDataOffset);
}

LAZBlockReader::~LAZBlockReader() {
	m_unzipper->close();
	delete m_unzipper;

	if (m_stream) {
		m_stream->close();
		delete m_stream;
	}

	if (m_file) {
		fclose(m_file);
		delete m_file;
	}

	if (m_streamBuffer) {
		delete m_streamBuffer;
	}

	delete[] m_lz_point;
	delete[] m_lz_point_data;
}
