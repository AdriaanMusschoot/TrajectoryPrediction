using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TMPro;
using UnityEngine;

public class PhysicsSimulation : MonoBehaviour
{
    //Physics parameters
    [SerializeField]
    float _mass;
    [SerializeField]
    float _dragCoefficient;
    [SerializeField]
    Vector3 _startVelocity;
    const float _gravity = -9.81f;
    float _gravityForce;
    private Vector3 _velocity;
    float _airDensity = 1.2f;
    //Collision parameters
    private const float _ballRadius = 0.5f;
    private const float _surfaceArea = _ballRadius * _ballRadius * (float)Mathf.PI;
    struct myPlane
    {
        public Vector3 normal;
        public float d;
        public float bounceForce;
    }
    private myPlane[] _planeArr;

    //Prediction parameters
    Vector3 _predictedPosition;
    Vector3 _predictedVelocity;
    List<Vector3> _predictedPoints = new List<Vector3>();
    [SerializeField]
    float _nrOfStepsInFuture;

    //LineRendererComponents
    GameObject _newLine;
    LineRenderer _trajectoryRenderer;

    //State
    enum State
    {
        Waiting,
        Shooting
    }
    State _currentState = State.Waiting;

    private void Start()
    {
        SetupMassRelated();
        _velocity = _startVelocity;

        //Set up the planes
        _planeArr = new myPlane[6];
        _planeArr[0].normal = new Vector3(0.0f, -1.0f, 0.0f);
        _planeArr[0].d = 5.0f;
        _planeArr[0].bounceForce = 1.0f;

        _planeArr[1].normal = new Vector3(0.0f, 1.0f, 0.0f);
        _planeArr[1].d = 5.0f;
        _planeArr[1].bounceForce = 1.0f;

        _planeArr[2].normal = new Vector3(1.0f, 0.0f, 0.0f);
        _planeArr[2].d = 5.0f;
        _planeArr[2].bounceForce = 0.5f;
        
        _planeArr[3].normal = new Vector3(-1.0f, 0.0f, 0.0f);
        _planeArr[3].d = 5.0f;
        _planeArr[3].bounceForce = 0.7f;
        
        _planeArr[4].normal = new Vector3(0.0f, 0.0f, 1.0f);
        _planeArr[4].d = 5.0f;
        _planeArr[4].bounceForce = 0.9f;
        
        _planeArr[5].normal = new Vector3(0.0f, 0.0f, -1.0f);
        _planeArr[5].d = 5.0f;
        _planeArr[5].bounceForce = 1.1f;


        //Set up the linerenderer
        _newLine = new GameObject("LineRenderer");
        _trajectoryRenderer = _newLine.AddComponent<LineRenderer>();
        _trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _trajectoryRenderer.startWidth = 0.2f;
        _trajectoryRenderer.endWidth = 0.2f;
        Color startLine = new Color(85.0f / 255.0f, 37.0f / 255.0f, 130.0f / 255.0f);
        Color endLine = new Color(253.0f / 255.0f, 185.0f / 255.0f, 39.0f / 255.0f);
        _trajectoryRenderer.startColor = startLine;
        _trajectoryRenderer.endColor = endLine;

        //Set up the UI
        _fireVelocity.text = _startVelocity.ToString();
        _currentMass.text = _mass.ToString();
        _stepsFuture.text = _nrOfStepsInFuture.ToString();
        _airResistance.text = _dragCoefficient.ToString();
    }

    private void SetupMassRelated()
    {
        _gravityForce = _mass * _gravity;
    }

    private void FixedUpdate()
    {
        switch (_currentState)
        {
            case State.Waiting:
                _velocity = _startVelocity;
                break;
            case State.Shooting:
                Vector3 temp = transform.position;
                UpdateBall(ref _velocity, ref temp, Time.deltaTime);
                transform.position = temp;
                break;
        }

        PredictTrajectory();
    }

    private void PredictTrajectory()
    {
        _predictedPoints.Clear();

        _predictedPosition = transform.position;
        _predictedVelocity = _velocity;
        _predictedPoints.Add(_predictedPosition);
        for (int idx = 0; idx < _nrOfStepsInFuture; ++idx)
        {
            UpdateBall(ref _predictedVelocity, ref _predictedPosition, Time.deltaTime);
            _predictedPoints.Add(_predictedPosition);
        }

        _trajectoryRenderer.positionCount = _predictedPoints.Count;
        _trajectoryRenderer.SetPositions(_predictedPoints.ToArray());
    }

    private void UpdateBall(ref Vector3 velocity, ref Vector3 position, float time)
    {
        float dragForceMagnitude = 0.5f * _dragCoefficient * _airDensity * _surfaceArea * velocity.magnitude * velocity.magnitude;
        Vector3 dragForceDirection = -velocity.normalized; // Opposite direction of velocity for drag force

        Vector3 dragAcceleration = dragForceMagnitude * dragForceDirection / _mass;

        velocity += new Vector3(0, _gravityForce, 0) * time;

        velocity += dragAcceleration * time;

        position += velocity * time;

        CustomCollision(ref velocity, ref position);
    }

    private void CustomCollision(ref Vector3 velocity, ref Vector3 position)
    {
        for(int idx = 0; idx < _planeArr.Length; ++idx)
        {
            float distance = Vector3.Dot(_planeArr[idx].normal, position) + _planeArr[idx].d;
            if (distance <= _ballRadius)
            {
                Vector3 collisionPoint = position + _planeArr[idx].normal * (_ballRadius - distance);
                Bounce(_planeArr[idx].normal, _planeArr[idx].bounceForce, collisionPoint, ref velocity, ref position);
            }
        }
    }
    private void Bounce(Vector3 normal, float bounceForce, Vector3 collisionPoint, ref Vector3 velocity, ref Vector3 position)
    {
        velocity = velocity - 2 * Vector3.Dot(velocity, normal) * normal;
        velocity *= bounceForce / _mass;

        //make sure the update starts when the ball is above the plane so it cannot instantly collide again
        position = collisionPoint;
    }

    //Parameter setup
    const float _stepSteps = 20;
    const float _stepMass = 1;
    const float _stepCoordinates = 1;
    const float _stepResistance = 0.05f;
    const float _maxVelocity = 100;
    const float _maxResistance = 1.0f;
    //UiStuff
    [SerializeField]
    private TextMeshProUGUI _fireVelocity;
    [SerializeField]
    private TextMeshProUGUI _currentMass;
    [SerializeField]
    private TextMeshProUGUI _stepsFuture;
    [SerializeField]
    private TextMeshProUGUI _fireButton;
    [SerializeField]
    private TextMeshProUGUI _airResistance;
    public void IncrementSteps()
    {
        _nrOfStepsInFuture += _stepSteps;
        _stepsFuture.text = _nrOfStepsInFuture.ToString();
    }
    public void DecrementSteps()
    {
        if (_nrOfStepsInFuture > _stepSteps)
        {
            _nrOfStepsInFuture -= _stepSteps;
            _stepsFuture.text = _nrOfStepsInFuture.ToString();
        }
    }
    public void IncrementMass()
    {
        _mass += _stepMass;
        SetupMassRelated();
        _currentMass.text = _mass.ToString();
    }
    public void Decrementmass()
    { 
        if (_mass > _stepMass)
        {
            _mass -= _stepMass;
            SetupMassRelated();
            _currentMass.text = _mass.ToString();
        }
    }
    public void IncrementX()
    {
        if (_startVelocity.x < _maxVelocity)
        {
            _startVelocity.x += _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }
    public void IncrementY()
    {
        if (_startVelocity.y < _maxVelocity)
        {
            _startVelocity.y += _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }
    public void IncrementZ()
    {
        if (_startVelocity.z < _maxVelocity)
        {
            _startVelocity.z += _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }
    public void DecrementX()
    {
        if (_startVelocity.x > -_maxVelocity)
        {
            _startVelocity.x -= _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }
    public void DecrementY()
    {
        if (_startVelocity.y > -_maxVelocity)
        {
            _startVelocity.y -= _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }
    public void DecrementZ()
    {

        if (_startVelocity.z > -_maxVelocity)
        {
            _startVelocity.z -= _stepCoordinates;
            _fireVelocity.text = _startVelocity.ToString();
        }
    }    

    public void IncrementResistance()
    {
        if (_dragCoefficient < _maxResistance)
        {
            _dragCoefficient += _stepResistance;
            _airResistance.text = _dragCoefficient.ToString();
        }
    }
    public void DecrementResistance()
    {
        if (_dragCoefficient > 0.001)
        {
            _dragCoefficient -= _stepResistance;
            _airResistance.text = _dragCoefficient.ToString();
        }
    }

    public void ToggleShootingOrWaiting()
    {
        switch(_currentState)
        {
            case State.Waiting:
                _currentState = State.Shooting;
                _fireButton.text = "RESET";
                break;
            case State.Shooting:
                _currentState = State.Waiting;
                transform.position = Vector3.zero;
                _velocity = _startVelocity;
                _fireButton.text = "SHOOT";
                break;
        }
    }
}