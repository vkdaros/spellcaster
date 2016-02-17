using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using PDollarGestureRecognizer;

public class GameLogic : MonoBehaviour {
    private const string DEFAULT_GESTURES_PATH = "gestureSet/default/";

    // Link exposed to Unity editor.
    public Transform gestureOnScreenPrefab;

    private List<Gesture> trainingSet = new List<Gesture>();

    private List<Point> points = new List<Point>();
    private int strokeId;

    private Vector3 virtualKeyPosition = Vector2.zero;
    private Rect drawArea;

    private RuntimePlatform platform;
    private int vertexCount;

    private List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
    private LineRenderer currentGestureLineRenderer;

    //GUI
    private string message;
    private bool drawing;

    void Start() {
        platform = Application.platform;
        drawing = false;
        strokeId = -1;
        vertexCount = 0;

        // Invisible area over spell book page.
        //drawArea = new Rect( 80, 50, 380, 380 );
		drawArea = new Rect( 0, 0, Screen.width, Screen.height * 0.7f );

        //Load pre-made gestures.
        LoadDefaultGestures();

        //Load user custom gestures.
        LoadCustomGestures();
    }

    void Update() {
        UpdateVirtualKeyPosition();

        if ( !drawing && Input.GetMouseButton( 0 ) &&
             drawArea.Contains( virtualKeyPosition ) ) {

            OnDrawingStarted();
        }
        else if ( drawing &&
                  Input.GetMouseButton( 0 ) &&
                  drawArea.Contains( virtualKeyPosition ) ) {

            OnDrawing();
        }
        else if ( drawing &&
                  ( !Input.GetMouseButton( 0 ) ||
                    !drawArea.Contains( virtualKeyPosition ) ) ) {

            OnDrawingFinished();
        }
    }

    private void OnDrawingStarted() {
        //Debug.Log( "[spellcaster] OnDrawingStarted." );

        ClearTrace();

        drawing = true;
        strokeId++;
        vertexCount = 0;

        Transform tmpGesture = Instantiate( gestureOnScreenPrefab,
                                            transform.position,
                                            transform.rotation ) as Transform;
        currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();
        gestureLinesRenderer.Add( currentGestureLineRenderer );
    }

    private void OnDrawing() {
        //Debug.Log( "[spellcaster] OnDrawing." );

        points.Add( new Point( virtualKeyPosition.x, -virtualKeyPosition.y, strokeId ) );

        vertexCount++;
        currentGestureLineRenderer.SetVertexCount( vertexCount );

        Vector3 rendererPosition = new Vector3( virtualKeyPosition.x,
                                                virtualKeyPosition.y,
                                                -1 );
        currentGestureLineRenderer.SetPosition( vertexCount - 1, rendererPosition );
    }

    private void OnDrawingFinished() {
        //Debug.Log( "[spellcaster] OnDrawingFinished." );

        drawing = false;
        strokeId = -1;

        AnalyzeGesture();
        //ClearTrace();
    }

    // Update virtualKeyPosition with touch or mouse position.
    private void UpdateVirtualKeyPosition() {
        if ( platform == RuntimePlatform.Android ||
             platform == RuntimePlatform.IPhonePlayer ) {

            if ( Input.touchCount > 0 ) {
                virtualKeyPosition = new Vector3( Input.GetTouch( 0 ).position.x,
                                                  Input.GetTouch( 0 ).position.y );
            }
        } else {
            if ( Input.GetMouseButton( 0 ) ) {
                virtualKeyPosition = new Vector3( Input.mousePosition.x,
                                                  Input.mousePosition.y );
            }
        }
        virtualKeyPosition = Camera.main.ScreenToWorldPoint ( virtualKeyPosition );
    }

    void OnGUI() {
        GUI.Label( new Rect( 10, Screen.height - 40, 500, 50 ), message );

        string tPosition = "( " + virtualKeyPosition.x + "; " + virtualKeyPosition.y + ")";
        GUI.Label( new Rect( 10, Screen.height - 60, 500, 70 ), tPosition );
    }

    private void AnalyzeGesture() {
        //Debug.Log( "AnalyzeGesture: points.Count: " + points.Count );
        Gesture candidate = new Gesture( points.ToArray() );
        Result gestureResult = PointCloudRecognizer.Classify( candidate, trainingSet.ToArray() );

        message = gestureResult.GestureClass + " " + gestureResult.Score;
    }

    private void ClearTrace() {
        points.Clear();

        foreach ( LineRenderer lineRenderer in gestureLinesRenderer ) {
            lineRenderer.SetVertexCount( 0 );
            Destroy( lineRenderer.gameObject );
        }

        gestureLinesRenderer.Clear();
    }

    void LoadDefaultGestures() {
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>( DEFAULT_GESTURES_PATH );
        foreach ( TextAsset gestureXml in gesturesXml ) {
            trainingSet.Add( GestureIO.ReadGestureFromXML( gestureXml.text ) );
        }
    }

    void LoadCustomGestures() {
        string[] filePaths = Directory.GetFiles( Application.persistentDataPath, "*.xml" );
        foreach ( string filePath in filePaths ) {
            trainingSet.Add( GestureIO.ReadGestureFromFile( filePath ) );
        }
    }
}

