public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}



public void Main() {
    IMyThrust thrusters = GridTerminalSystem.GetBlockWithName("Thruster") as IMyThrust;
    IMyTextPanel screen = GridTerminalSystem.GetBlockWithName("ThrustScreen") as IMyTextPanel;

    screen.BackgroundColor = new Color(0, 0, 0, 0);
    screen.BackgroundAlpha = 0.0f;
    screen.FontColor = new Color(255, 255, 255, 255);
    screen.FontSize = 1.0f;
    float g = 9.81f;

    foreach (IMyThrust t in thrusters){
        float currentThrust = t.CurrentThrust;
        Vector3I thrustDirection = t.GridThrustDirection;
        float maxEffectiveThrust = t.MaxEffectiveThrust;
        float maxThrust = t.MaxThrust;
        float thrustOverride = t.ThrustOverride;
        float thrustOverridePercentage = t.ThrustOverridePercentage;
        bool enabled = t.Enabled;
        bool isWorking = t.IsWorking;
        bool isFunctional = t.IsFunctional;
        MyBlockOrientation orientation = t.Orientation;
    }

    // For electric thrusters it is straight forward. They always got 30% thrust and the other 70% scale linear to the atmospheric density clamped to 0 and 1.
    ThrustEfficiency = 0.3 + 0.7 *(1 - max(0, min(1, Atmosphere)))


    // For atmospheric thrusters it is:
    ThrustEfficiency = max(0, min(0.7, Atmosphere - 0.3)) / 0.7


    Vector3D Transform(Vector3D position, MatrixD matrix)
    Vector3D velocity;
    MatrixD mat;

    Vector3D localVelocity = Vector3D.Transform(velocity, MatrixD.Transpose(mat));
    double forwardSpeed = -localVelocity.Z;


    

}

public double getShipDirectionalSpeed(IMyCockpit cockpit, Base6Directions.Direction direction) {
	//get the velocity of the ship as a vector
	Vector3D velocity = cockpit.GetShipVelocities().LinearVelocity;

	//given a direction calculate the "length" for that direction, lenght is the speed in this case
	return velocity.Dot(cockpit.WorldMatrix.GetDirectionVector(direction));
}