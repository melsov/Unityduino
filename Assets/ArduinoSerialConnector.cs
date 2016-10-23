using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System;

public class ArduinoSerialConnector : MonoBehaviour {

    SerialPort stream = new SerialPort("COM5", 115200);

	void Awake () {
        stream.ReadTimeout = 50;
        stream.Open();
        StartCoroutine(pingDuino());
	}

    private IEnumerator pingDuino() {
        for(int i=0;i<20;++i) {
            writeToArduino("PING");
            yield return new WaitForSeconds(1f);
            print(ReadFromArduino(20));
        }
    }

    public void writeToArduino(string message) {
        stream.WriteLine(message);
        stream.BaseStream.Flush();
    }

    public string ReadFromArduino(int timeout = 0) {
        stream.ReadTimeout = timeout;
        try {
            return stream.ReadLine();
        } catch (TimeoutException) {
            return null;
        }
    }

    public IEnumerator AsynchronousReadFromArduino(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity) {
        DateTime initialTime = DateTime.Now;
        DateTime nowTime;
        TimeSpan diff = default(TimeSpan);

        string dataString = null;
        
        do {
            try {
                dataString = stream.ReadLine();
            } catch (TimeoutException) {
            dataString = null;
            }

            if (dataString != null) {
                callback(dataString);
                yield return null;
            } else
                yield return new WaitForSeconds(0.05f);

            nowTime = DateTime.Now;
            diff = nowTime - initialTime;

        } while (diff.Milliseconds < timeout);

        if (fail != null)
        fail();
        yield return null;
    }

	void Update () {
	
	}




}
