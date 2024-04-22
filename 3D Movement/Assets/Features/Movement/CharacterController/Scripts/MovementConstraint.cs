namespace NuiN.Movement
{
    public class MovementConstraint
    {
        public readonly bool canMove;
        public readonly bool canRotate;

        public MovementConstraint(bool canMove, bool canRotate)
        {
            this.canMove = canMove;
            this.canRotate = canRotate;
        }
    }
}