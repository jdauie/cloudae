#pragma once

#include <stdio.h>
#include <msclr/marshal_cppstd.h>

#include "lasunzipper.hpp"

using namespace System;

namespace CloudAE {
	namespace Interop {
		namespace LAZ {

public ref class LAZInterop
{
public:
	void unzip(System::String ^path) {
		msclr::interop::marshal_context context;
		const char* pathStr = context.marshal_as<const char*>(path);

		FILE* pFile = fopen(pathStr, "r");
		if (pFile != NULL)
		{
			unsigned char* bytes;
			int num;
			LASzip laszip_dec;
			if (laszip_dec.unpack(bytes, num))
			{
				LASunzipper* unzipper = new LASunzipper();
				unzipper->open(pFile, &laszip_dec);

				//// allocating the data to write into
				//data.setup(laszip_dec.num_items, laszip_dec.items);

				//unsigned int num_errors = 0;
				//int c = 0;

				//for (int i = 0; i < 1000; i++)
				//{
				//	unzipper->read(data.point);
				//	for (int j = 0; j < data.point_size; j++)
				//	{
				//		if (data.point_data[j] != c)
				//		{
				//			//log("%d %d %d != %d\n", i, j, data.point_data[j], c);
				//			num_errors++;
				//			if (num_errors > 20) break;
				//		}
				//		else
				//		{
				//			fprintf(
				//		}
				//		c++;
				//	}
				//	if (num_errors > 20) break;
				//}

				unzipper->close();
			}

			fclose(pFile);
		}
	}
};

		}
	}
}
