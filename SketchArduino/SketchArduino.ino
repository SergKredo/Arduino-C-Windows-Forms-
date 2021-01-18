#include <SoftwareSerial.h>
#include <HID.h>
#include <EEPROM.h>
#include <Adafruit_PWMServoDriver.h>
#include <ServoDriverSmooth.h>
#include <ServoSmooth.h>
#include <smoothUtil.h>

#include <Servo.h>

// переменные для фильтра калмана
float varVolt = 0.6;  // среднее отклонение (ищем в excel)
float varProcess = 100; // скорость реакции на изменение (подбирается вручную)
float Pc = 0.0;
float G = 0.0;
float P = 1.0;
float Xp = 0.0;
float Zp = 0.0;
float Xe = 0.0;
// переменные для калмана


float filtSignal = 0.0;   // фильтрованный сигнал
float filtSignalOtherDetector = 0.0;   // фильтрованный сигнал с другого датчика

// бегущее среднее, ещё более оптимальный вариант предыдущего фильтра
float expRunningAverage(float newVal)
{
	static float filVal = 0;
	filVal += (newVal - filVal) * 0.2;
	return filVal;
}

// медиана на 3 значения со своим буфером
float median(float newVal) {
	static float buf[3];
	static byte count = 0;
	buf[count] = newVal;
	if (++count >= 3) count = 0;

	float a = buf[0];
	float b = buf[1];
	float c = buf[2];

	float middle;
	if ((a <= b) && (a <= c)) {
		middle = (b <= c) ? b : c;
	}
	else {
		if ((b <= a) && (b <= c)) {
			middle = (a <= c) ? a : c;
		}
		else {
			middle = (a <= b) ? a : b;
		}
	}
	return middle;
}

Servo servo;
long val;
int pinVol = 0;
int pinVolOtherDetector = 3;
float valPinServo = 0;
float valPinOtherDetector = 0;
float valKalman = 0;
long valPlus = 0;
void setup()
{
	servo.attach(9);
	Serial.begin(115200);
}

void loop()
{
	val = Serial.parseFloat();
	if (val != 0)
	{
		valPinServo = analogRead(pinVol);
		valPinOtherDetector = analogRead(pinVolOtherDetector);
		measure();
		//valKalman = filter(valPinServo);
		//Serial.println("$");
		Serial.print(map(valPinServo,0, 1023, 0.00, 10000));
		//Serial.print(" ");
		Serial.print(";   ");
		//Serial.println(fil_var);
		Serial.print(map(valPinOtherDetector, 0, 1023, 0.00, 10000));
		Serial.print(";   ");

		//Serial.println(valKalman);
		Serial.print(map(filtSignal, 0, 1023, 0.00, 10000));
		Serial.print(";   ");

		Serial.println(map(filtSignalOtherDetector, 0, 1023, 0.00, 10000));
		//Serial.println(";");
		valPlus = --val;
		servo.write(valPlus);
	}

}
float filter(float val)
{  //функция фильтрации
	Pc = P + varProcess;
	G = Pc / (Pc + varVolt);
	P = (1 - G) * Pc;
	Xp = Xe;
	Zp = Xp;
	Xe = G * (val - Zp) + Xp; // "фильтрованное" значение
	return(Xe);
}

// измерение с заданным периодом
void measure() {
	static uint32_t tmr;
	if (millis() - tmr >= 5) {
		tmr = millis();
		filtSignal = median(valPinServo);
		filtSignal = filter(filtSignal);

		filtSignalOtherDetector = median(valPinOtherDetector);
		filtSignalOtherDetector = filter(filtSignalOtherDetector);
		//filtSignal = expRunningAverage(filtSignal); // + к медиане
	}
}
