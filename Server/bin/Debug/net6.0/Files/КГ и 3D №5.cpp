// КГ и 3D №5.cpp : Определяет точку входа для приложения.
//

#include "framework.h"
#include "КГ и 3D №5.h"
#include <math.h>
#include <vector>
#include <stdlib.h>
#include <stdio.h>
#include <conio.h>
#include <math.h>
#include <complex>
using namespace std;

#define MAX_LOADSTRING 100
#define Pi 3.141592653589

// Глобальные переменные:
HINSTANCE hInst;                                // текущий экземпляр
WCHAR szTitle[MAX_LOADSTRING];                  // Текст строки заголовка
WCHAR szWindowClass[MAX_LOADSTRING];            // имя класса главного окна

#define pi 3.14159265
double t = 0.78539816249999916, f = 5.0614548249999975, R = 1; 

double halfcube[8][4]{ { 10,10,10,1 },{ 10,0,0,1 },{ 0,10,0,1 },{ 0,0,10,1 },{ 10,10,10,1 },{ 0,10,0,1 },{ 0,0,10,1 },{ 10,0,0,1 } };  // РјРёСЂРѕРІС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ РєСѓР±Р°, СЂР°Р·СЂРµР·Р°РЅРЅРѕРіРѕ РїРѕ РґРёР°РіРѕРЅР°Р»Рё  // A -> B -> C -> D -> A -> D1 -> D -> D1 -> C1 -> C -> C1 -> B
double halfcube_2D[12][4];
double offset = 0;

double vid[4][4]{ { -sin(t), -cos(f) * cos(t), -sin(f) * cos(t), 0 },
				  { cos(t),  -cos(f) * sin(t), -sin(f) * sin(t), 0 },
				  { 0,                 sin(f),          -cos(f), 0 },
				  { 0,                      0,                R, 1 } };

double per[4][4]{ { 25,    0, 0, -0.1 },
				  { 0,    25, 0, -0.1 },
				  { 0,     0, 0, -0.1 },
				  { 500, 310, 0, -0.1 } };


using namespace std;
using coord = double;
using edge = vector<coord>;
using drawings = vector<edge>;
//drawings figure;

void UpdateVid()
{
	vid[0][0] = -sin(t);
	vid[0][1] = -cos(f) * cos(t);
	vid[0][2] = -sin(f) * cos(t);
	vid[1][0] = cos(t);
	vid[1][1] = -cos(f) * sin(t);
	vid[1][2] = -sin(f) * sin(t);
	vid[2][1] = sin(f);
	vid[2][2] = -cos(f);
	vid[3][2] = R;

}


void UpdatePer()
{
	int n;
	for (int i = 0; i < sizeof(halfcube_2D) / 32; i++)
		if (halfcube_2D[i][3] != 1)
		{
			n = 1 - halfcube_2D[i][3];
			halfcube_2D[i][3] = 1;
			halfcube_2D[i][0] += n;
			halfcube_2D[i][1] += n;
		}
}

/*void UpdateCoord()
{
	int n;
	for (int i = 0; i < sizeof(halfcube) / 32; i++)
	{
		per[3][0] += offset;


	}
}*/

void Copy(const double temp[][4], double halfcube_2D[][4])
{
	for (int i = 0; i < sizeof(halfcube) / 32; i++)
		for (int j = 0; j < 4; j++)
			halfcube_2D[i][j] = temp[i][j];
}



void transformation(double halfcube_2D[][4], double transf[4][4])
{
	double x[4];

	for (int i = 0; i < sizeof(halfcube) / 32; i++)
	{
		for (int j = 0; j < 4; j++)
			x[j] = halfcube_2D[i][0] * transf[0][j] + halfcube_2D[i][1] * transf[1][j] + halfcube_2D[i][2] * transf[2][j] + halfcube_2D[i][3] * transf[3][j];

		for (int j = 0; j < 4; j++)
			halfcube_2D[i][j] = x[j];
	}
}



void draw(HDC hdc, double halfcube_2D[][4])
{
	MoveToEx(hdc, halfcube_2D[0][0], halfcube_2D[0][1], NULL);

	HGDIOBJ original = NULL; //  Initializing original object
	original = SelectObject(hdc, GetStockObject(DC_PEN)); // Saving the original object


	for (int i = 1; i < sizeof(halfcube) / 32; i++)
	{
		LineTo(hdc, halfcube_2D[i][0], halfcube_2D[i][1]);
		SetDCPenColor(hdc, RGB(i * 100, i * 50, i * 80));

	}

	SelectObject(hdc, original); // Restoring the original object
	DeleteObject(original);
}

ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK    About(HWND, UINT, WPARAM, LPARAM);

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPWSTR    lpCmdLine,
	_In_ int       nCmdShow)
{
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	LoadStringW(hInstance, IDC_MY3D5, szWindowClass, MAX_LOADSTRING);
	MyRegisterClass(hInstance);

	if (!InitInstance(hInstance, nCmdShow))
	{
		return FALSE;
	}

	HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_MY3D5));

	MSG msg;

	while (GetMessage(&msg, nullptr, 0, 0))
	{
		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}

	return (int)msg.wParam;
}


ATOM MyRegisterClass(HINSTANCE hInstance)
{
	WNDCLASSEXW wcex;

	wcex.cbSize = sizeof(WNDCLASSEX);

	wcex.style = CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc = WndProc;
	wcex.cbClsExtra = 0;
	wcex.cbWndExtra = 0;
	wcex.hInstance = hInstance;
	wcex.hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_MY3D5));
	wcex.hCursor = LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wcex.lpszMenuName = MAKEINTRESOURCEW(IDC_MY3D5);
	wcex.lpszClassName = szWindowClass;
	wcex.hIconSm = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));

	return RegisterClassExW(&wcex);
}


BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
	hInst = hInstance; 

	HWND hWnd = CreateWindowW(szWindowClass, szTitle, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, hInstance, nullptr);

	if (!hWnd)
	{
		return FALSE;
	}

	ShowWindow(hWnd, nCmdShow);
	UpdateWindow(hWnd);

	return TRUE;
}


LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{


	switch (message)
	{
	case WM_COMMAND:
	{
		int wmId = LOWORD(wParam);

		switch (wmId)
		{
		case IDM_ABOUT:
			DialogBox(hInst, MAKEINTRESOURCE(IDD_ABOUTBOX), hWnd, About);
			break;
		case IDM_EXIT:
			DestroyWindow(hWnd);
			break;
		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	break;

	case WM_KEYDOWN:
	{

		int wmId = LOWORD(wParam);

		switch (wmId)
		{
		case VK_UP:
			f += pi / 36; // f - РіРѕСЂРёР·РѕРЅС‚Р°Р»СЊРЅР°СЏ РїР»РѕСЃРєРѕСЃС‚СЊ
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_DOWN:
			f -= pi / 36;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_RIGHT:
			t -= pi / 36; // t - РІРµСЂС‚РёРєР°Р»СЊРЅР°СЏ РїР»РѕСЃРєРѕСЃС‚СЊ
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_LEFT:
			t += pi / 36;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;


		case VK_NUMPAD4: // СЃРјРµС‰РµРЅРёРµ
			per[3][0] -= 10; // РїРѕ x
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_NUMPAD6:
			per[3][0] += 10;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_NUMPAD2:
			per[3][1] += 10; // РїРѕ y
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_NUMPAD8:
			per[3][1] -= 10;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;

		case VK_SUBTRACT: // РїРµСЂСЃРїРµРєС‚РёРІРЅРѕРµ РёР·РјРµРЅРµРЅРёРµ
			per[2][3]--;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;
		case VK_ADD:
			per[2][3]++;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;

		case VK_CONTROL: // default
			per[2][3] = 0;
			f = 5.0614548249999975;
			t = 0.78539816249999916;
			UpdateVid();
			InvalidateRect(hWnd, 0, true);
			UpdateWindow(hWnd);
			break;

		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	break;

	case WM_PAINT:
	{
		PAINTSTRUCT ps;
		HDC hdc = BeginPaint(hWnd, &ps);
		
		Copy(halfcube, halfcube_2D);

		transformation(halfcube_2D, vid);
		transformation(halfcube_2D, per);

		UpdatePer();

		draw(hdc, halfcube_2D);

		EndPaint(hWnd, &ps);
	}
	break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}


INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(lParam);
	switch (message)
	{
	case WM_INITDIALOG:
		return (INT_PTR)TRUE;

	case WM_COMMAND:
		if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL)
		{
			EndDialog(hDlg, LOWORD(wParam));
			return (INT_PTR)TRUE;
		}
		break;
	}
	return (INT_PTR)FALSE;
}
