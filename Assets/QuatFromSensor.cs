using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System;
using System.Collections.Generic;

public class QuatFromSensor : MonoBehaviour {
    SerialPort stream = new SerialPort("COM7", 115200);
    private Rigidbody rb;
    private Quaternion rot;
    private Quaternion calOffset = Quaternion.identity;

	void Awake () {
        rb = GetComponent<Rigidbody>();
        rot = rb.rotation;
        stream.ReadTimeout = 50;
        startStream();
        //StartCoroutine(calibrate());
        StartCoroutine(readDuino(s => updateRotation(s)));
    }

    private IEnumerator calibrate() {
        print("place sensor upright and forwards");
        yield return new WaitForSeconds(1f);
        List<Quaternion> readings = new List<Quaternion>();
        for(int i = 0; i < 200; ++i) {
            readings.Add(fromString(quatReading()));
            yield return new WaitForEndOfFrame();
        }
        print("calibration done");
        calOffset = avgQuat(readings);
        StartCoroutine(readDuino(s => updateRotation(s)));
    }

    //private Quaternion calcAvg(List<Quaternion> rotationlist) {
    //    if (rotationlist.Count == 0)
    //        throw new ArgumentException();

    //    float x = 0, y = 0, z = 0, w = 0;
    //    foreach (Quaternion q in rotationlist) { 
    //        x += q.x; y += q.y; z += q.z; w += q.w;
    //    }
    //    float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
    //    Quaternion quat = new Quaternion(x * k, y * k, z * k, w * k);
    //    return quat;
    //}

/*
 * gamedev.stackexchange.com/questions/119688/calculate-average-of-arbitrary-amount-of-quaternions-recursion
*/ 
    private Quaternion avgQuat(List<Quaternion> ternions) {
        if(ternions.Count == 0) {
            return default(Quaternion);
        }
        if(ternions.Count == 1) {
            return ternions[0];
        }
        int count = ternions.Count;
        List<Quaternion> firstHalf = ternions.GetRange(0, count / 2);
        List<Quaternion> secondHalf = ternions.GetRange(count / 2, count - count / 2);

        Quaternion q1 = avgQuat(firstHalf);
        Quaternion q2 = avgQuat(secondHalf);
        if (q1 == default(Quaternion)) {
            return q2;
        } else if (q2 == default(Quaternion)) {
            return q1;
        }
        return Quaternion.Lerp(q1, q2, firstHalf.Count / ((float)secondHalf.Count));
    }

    private void startStream() {
        if (!stream.IsOpen) {
            stream.Open();
        }
    }
    private void shutdown() {
        if (stream.IsOpen) {
            stream.Close();
        }
    }
    
    public void FixedUpdate() {
        try {
            rb.MoveRotation(rot);
        } catch(System.Exception e) {
            print("Quaternion prob: " + e.ToString());
        }
    }
/*
TODO:
1. Just collect all the data in some handy Vectors/Quats
2. Try the Processing bunny way with a transformation matrix:

*/
    private void updateRotation(string s) {
        //rot = fromRollPitchYaw(s);
        rot = (fromString(s)); // * Quaternion.Inverse(calOffset));
    }

    private Quaternion fromRollPitchYaw(string s) {
        string[] compos = s.Split(' ');
        if(!compos[0].Equals("ORI")) {
            return rot;
        }
        Vector3 euler;
        euler.x = float.Parse(compos[1]);
        euler.y = float.Parse(compos[2]);
        euler.z = float.Parse(compos[3]);
        return Quaternion.Euler(euler);
    }

    private Quaternion fromString(string s) {
        Quaternion q = new Quaternion();
        string[] compos = s.Split(' ');
        if(!compos[0].Equals("QUAT")) {
            return rot;
        }
        // string "w x y z"
        foreach(string c in compos) {
            print(c);
        }
        q.w = float.Parse(compos[1]);
        q.x = float.Parse(compos[2]);
        q.y = float.Parse(compos[3]);
        q.z = float.Parse(compos[4]);
        return normalizeQuat(q);
    }

    private Quaternion normalizeQuat(Quaternion q) {
        return Quaternion.Euler(q.eulerAngles);
    }

    public string ReadFromArduino(int timeout = 0) {
        stream.ReadTimeout = timeout;
        try {
            return stream.ReadLine();
        } catch (TimeoutException) {
            return null;
        }
    }

    private string quatReading() {
        string data = null;
        try {
            data = stream.ReadLine();
        } catch (TimeoutException) {
        } catch (System.Exception e) {
            print("io excptn: " + e.ToString());
        }
        return data;
    }

    public IEnumerator readDuino(Action<string> callback, Action fail = null, float timeoutSeconds = 60f * 5f) {
        float lastReadTimeSeconds = Time.fixedTime;
        string data = null;
        do {
            data = quatReading();
            if(data != null) {
                callback(data);
                lastReadTimeSeconds = Time.fixedTime;
                yield return null;
            } else {
                yield return new WaitForFixedUpdate();
            }
        } while (lastReadTimeSeconds + timeoutSeconds > Time.fixedTime);
        
    }

    public void OnEnable() {
        startStream();
    }

    public void OnDisable() {
        shutdown();
    }
    
    public void OnDestroy() {
        shutdown();
    }



}
