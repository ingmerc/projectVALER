using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Networking;
using System.Collections;
using System.Globalization;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
public class IntegratedScript : MonoBehaviour
{
    public XRNode headNode = XRNode.Head;
    public XRNode rightHandNode = XRNode.RightHand;
    public TMP_Text UiText = null;
    public TMP_Text InputUiText = null;

    private string postUrl = "http://192.168.1.104:5001/upload";
    private float lastPostTime;

    private string uniqueIdentifier;
    private bool isFirstRow = true;

    private OVRPlayerController playerController;
    private CharacterController characterController;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private List<string> dataLines = new List<string>();
    private int fileCounter = 1;

    private string[] Codes = new string[10];
    private int codeIndex = 0;
    private string Nr = null;
    private int NrIndex = 0;

    private int respawnCount = 0;
    private int maxRespawnCount = 2;

    private bool correctCodeEntered = false;

    void Start()
    {
        uniqueIdentifier = Guid.NewGuid().ToString();

        playerController = FindObjectOfType<OVRPlayerController>();
        if (playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            initialPosition = playerController.transform.position;
            initialRotation = playerController.transform.rotation;
        }
        else
        {
            Debug.LogError("OVRPlayerController not found in the scene.");
        }

        GenerateCodes();
        DisplayNextCode();
    }

    void Update()
    {
        if (dataLines.Count > 0 && correctCodeEntered)
        {
            StartCoroutine(PostData());
            lastPostTime = Time.time;

            dataLines.Clear();
            isFirstRow = true;
            fileCounter++;

            // Reset the flag
            correctCodeEntered = false;

            // Change the current code
            ChangeCode();

            // Perform respawn
            RespawnPlayer();
            DisplayNextCode();
        }
        else
        {
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(headNode);
            Vector3 headPosition;
            Quaternion headOrientation;

            if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headPosition) &&
                headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out headOrientation))
            {
                InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(rightHandNode);
                Vector3 controllerPosition;
                Quaternion controllerOrientation;

                if (rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPosition) &&
                    rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerOrientation))
                {
                    SaveToDataLines(Time.time, headOrientation.eulerAngles, headPosition, controllerOrientation.eulerAngles, controllerPosition);
                }
            }
        }
    }

    bool IsInInitialPosition()
    {
        return Vector3.Distance(playerController.transform.position, initialPosition) < 0.1f &&
               Quaternion.Angle(playerController.transform.rotation, initialRotation) < 1.0f;
    }

    void SaveToDataLines(float timestamp, Vector3 headOrientation, Vector3 headPosition, Vector3 controllerOrientation, Vector3 controllerPosition)
   {
    CultureInfo culture = CultureInfo.InvariantCulture;

    // Intestazione della riga (aggiunta solo alla prima riga)
    string formattedLine = isFirstRow ?
        "Timestamp,Head_Pitch,Head_Yaw,Head_Roll,Head_X,Head_Y,Head_Z,Controller_Pitch,Controller_Yaw,Controller_Roll,Controller_X,Controller_Y,Controller_Z,Codice\n" :
        "";

    // Dati della riga
    formattedLine += $"{timestamp.ToString(culture)},{headOrientation.x.ToString("0.0", culture)},{headOrientation.y.ToString("0.0", culture)},{headOrientation.z.ToString("0.0", culture)}," +
                     $"{headPosition.x.ToString("0.0", culture)},{headPosition.y.ToString("0.0", culture)},{headPosition.z.ToString("0.0", culture)}," +
                     $"{controllerOrientation.x.ToString("0.0", culture)},{controllerOrientation.y.ToString("0.0", culture)},{controllerOrientation.z.ToString("0.0", culture)}," +
                     $"{controllerPosition.x.ToString("0.0", culture)},{controllerPosition.y.ToString("0.0", culture)},{controllerPosition.z.ToString("0.0", culture)}," +
                     $"{Codes[codeIndex]}\n";

    // Aggiungi la riga alla lista
    dataLines.Add(formattedLine);

    // Imposta isFirstRow a false dopo la prima riga
    isFirstRow = false;
    }


    IEnumerator PostData()
    {
        string fileName = $"orientationData_{fileCounter}_{uniqueIdentifier}.csv";
        string allData = string.Join("", dataLines.ToArray());
        System.IO.File.WriteAllText(GetFilePath(fileName), allData);

        WWWForm form = new WWWForm();
        byte[] fileData = System.IO.File.ReadAllBytes(GetFilePath(fileName));
        form.AddBinaryData("file", fileData, fileName, "text/csv");

        UnityWebRequest www = UnityWebRequest.Post(postUrl, form);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Data sent to the server successfully: " + www.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending data to the server: " + www.error);
        }
    }

    string GetFilePath(string fileName)
    {
        return System.IO.Path.Combine(Application.persistentDataPath, fileName);
    }

    void GenerateCodes()
    {
        Codes[0] = "57643";
        Codes[1] = "23522";
        Codes[2] = "12345";
        Codes[3] = "67890";
        Codes[4] = "98765";
        Codes[5] = "11111";
        Codes[6] = "99999";
        Codes[7] = "45678";
        Codes[8] = "87654";
        Codes[9] = "54321";
    }

    void DisplayNextCode()
    {
        UiText.text = Codes[codeIndex];
        Debug.Log("New Code: " + Codes[codeIndex]);
    }

    void RespawnPlayer()
    {
        characterController.enabled = false;
        playerController.transform.position = initialPosition;
        playerController.transform.rotation = initialRotation;
        characterController.enabled = true;
    }

    public void CodeFunctionComplex(string Number)
    {
        NrIndex++;
        Nr = Nr + Number;

        UpdateInputUiText();
    }

    void UpdateInputUiText()
    {
    if (InputUiText != null)
      {
        InputUiText.text = Nr;
      }
    }

    public void DeleteComplex()
    {
        NrIndex++;
        Nr = null;
        DisplayNextCode();
    }

    // Funzione chiamata quando si vuole eseguire la verifica del codice
    public void CheckCode()
    {
        if (Nr == Codes[codeIndex])
        {
            Debug.Log("Correct Code!");

            // Reset Nr and NrIndex
            Nr = null;
            NrIndex = 0;

            // Set the flag to indicate correct code entered
            correctCodeEntered = true;
        }
        else
        {
            Debug.Log("Wrong Code");
        }
    }

    // Funzione per cambiare il codice corrente
    void ChangeCode()
    {
        codeIndex++;

        if (codeIndex == 10)
        {
            codeIndex = 0;

            respawnCount++;

            if (respawnCount == maxRespawnCount)
            {
                RespawnPlayer();
                SceneManager.LoadScene(2);
                return;
            }
        }
    }
}
