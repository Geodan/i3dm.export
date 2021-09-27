using System.Collections.Generic;
using System.Numerics;
using i3dm.export.Extensions;

namespace i3dm.export.ECEF
{
    public static class Transforms
    {
        public static Dictionary<Axis, int[]> DegeneratePosition = new Dictionary<Axis, int[]>(){
            {Axis.NORTH, new []{-1, 0, 0}},
            {Axis.EAST, new []{0, 1, 0}},
            {Axis.UP, new []{0, 0, 1}},
            {Axis.SOUTH, new []{1, 0, 0}},
            {Axis.WEST, new []{0, -1, 0}},
            {Axis.DOWN, new []{0, 0, -1}},
        };

        public static Matrix4x4 GetHPRMatrixAtPosition(Vector3 position, float heading, float pitch, float roll, Ellipsoid ellipsoid, Axis firstAxis, Axis secondAxis) {
            var hprQuat = new Quaternion().FromHeadingPitchRoll(heading, pitch, roll);
            var hprMat = ECEFMath.Matrix4FromTranslationQuaternionRotationScale(Vector3.Zero, hprQuat, new Vector3(1.0f, 1.0f, 1.0f));
            var fixedFrame = LocalToFixed(position, ellipsoid, firstAxis, secondAxis);

            return Matrix4x4.Multiply(hprMat, fixedFrame);
        }

         public static Matrix4x4 LocalToFixed(Vector3 position, Ellipsoid ellipsoid, Axis axis1, Axis axis2) {
            var axis3 = GetVectorProductLocal(axis1, axis2);            
            var calculateVector = new Dictionary<Axis, Vector3>(){
                { Axis.EAST, new Vector3()},
                { Axis.NORTH, new Vector3()},
                { Axis.UP, new Vector3()},
                { Axis.WEST, new Vector3()},
                { Axis.SOUTH, new Vector3()},
                { Axis.DOWN, new Vector3()}
            };

            calculateVector[Axis.UP] = ellipsoid.GeodeticSurfaceNormal(position);

            var up = calculateVector[Axis.UP];
            var east = calculateVector[Axis.EAST];
            east.X = -position.Y;
            east.Y = position.X;
            east.Z = 0.0f;

            calculateVector[Axis.EAST] = Vector3.Normalize(east);
            calculateVector[Axis.NORTH] = Vector3.Cross(up,  calculateVector[Axis.EAST]);
            calculateVector[Axis.DOWN] = calculateVector[Axis.UP];
            calculateVector[Axis.WEST] = calculateVector[Axis.EAST];
            calculateVector[Axis.SOUTH] = calculateVector[Axis.NORTH];

            var result = CalculateVectorToMatrix4(calculateVector[axis1], calculateVector[axis2], calculateVector[axis3], position);
            return result;
        }

        private static Matrix4x4 CalculateVectorToMatrix4(Vector3 vector1, Vector3 vector2, Vector3 vector3, Vector3 vector4) 
        {
            var result = new Matrix4x4();
            result.M11 = vector1.X;
            result.M12 = vector1.Y;
            result.M13 = vector1.Z;
            result.M14 = 0.0f;

            result.M21 = vector2.X;
            result.M22 = vector2.Y;
            result.M23 = vector2.Z;
            result.M24 = 0.0f;

            result.M31 = vector3.X;
            result.M32 = vector3.Y;
            result.M33 = vector3.Z;
            result.M34 = 0.0f;

            result.M41 = vector4.X;
            result.M42 = vector4.Y;
            result.M43 = vector4.Z;
            result.M44 = 1.0f;

            return result;
        }

        private static Axis GetVectorProductLocal(Axis firstAxis, Axis secondAxis) {
            switch (firstAxis){
                case Axis.UP:
                    switch(secondAxis) {
                        case Axis.SOUTH:
                            return Axis.EAST;
                        case Axis.NORTH:
                            return Axis.WEST;
                        case Axis.WEST:
                            return Axis.SOUTH;
                        case Axis.EAST:
                            return Axis.NORTH;                        
                    }
                    break;
                case Axis.DOWN:
                    switch(secondAxis) {
                        case Axis.SOUTH:
                            return Axis.WEST;
                        case Axis.NORTH:
                            return Axis.EAST;
                        case Axis.WEST:
                            return Axis.NORTH;
                        case Axis.EAST:
                            return Axis.SOUTH;
                    }
                    break;
                case Axis.SOUTH:
                    switch(secondAxis) {
                        case Axis.UP:
                            return Axis.WEST;
                        case Axis.DOWN:
                            return Axis.EAST;
                        case Axis.WEST:
                            return Axis.DOWN;
                        case Axis.EAST:
                            return Axis.UP;
                    }
                    break;
                case Axis.NORTH:
                    switch(secondAxis) {
                        case Axis.UP:
                            return Axis.EAST;
                        case Axis.DOWN:
                            return Axis.WEST;
                        case Axis.WEST:
                            return Axis.UP;
                        case Axis.EAST:
                            return Axis.DOWN;
                    }
                    break;
                case Axis.WEST:
                    switch(secondAxis) {
                        case Axis.UP:
                            return Axis.NORTH;
                        case Axis.DOWN:
                            return Axis.SOUTH;
                        case Axis.NORTH:
                            return Axis.DOWN;
                        case Axis.SOUTH:
                            return Axis.UP;
                    }
                    break;
                case Axis.EAST:
                    switch(secondAxis) {
                        case Axis.UP:
                            return Axis.SOUTH;
                        case Axis.DOWN:
                            return Axis.NORTH;
                        case Axis.NORTH:
                            return Axis.UP;
                        case Axis.SOUTH:
                            return Axis.DOWN;
                    }
                    break;
            }

            return Axis.NORTH;
        }
    }
}