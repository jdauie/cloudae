#pragma once

#include "lasunzipper.hpp"

public class LAZBlockReader
{
public:

	LAZBlockReader(LASunzipper* unzipper, unsigned char** lz_point, unsigned char* lz_point_data, unsigned int lz_point_size);
    ~LAZBlockReader();

	int Read(unsigned char* buffer, int byteOffset, int byteCount);

private:

	LASunzipper* m_unzipper;
	unsigned char** m_lz_point;
	unsigned char* m_lz_point_data;
	unsigned int m_lz_point_size;

};


