using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.Tracks.Roads;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;

namespace DepotExtended.GoToDepot
{
    [SchemaVersion(1)]
    public class GoToDepotVehicleData: IVehicleDestination
    {
        private readonly Vehicle _vehicle;
        private float _distanceBeyondEnd;
        private float _enterVelocity = float.MinValue;
        private List<TrackUnitPoint> _trainPoints;
        public string Name { get; }

        public HashSet<TrackConnection> Stops { get; } = new();
        public TrackConnection TargetConnection { get; }
        public Vector3 Target { get; }
        public bool IsValid { get; private set; }
        public TurnAroundOverrideTask TurningTask { get; set; }
        public GoToDepotOverrideTask Task { get; }
        public VehicleDepot Depot { get; }
        public bool IsTrainEnteringDepot => _distanceBeyondEnd > 0; 

        internal void WriteEnteringData(StateBinaryWriter writer)
        {
            //saving only when trains are entering depot
            writer.WriteFloat(_distanceBeyondEnd);
            writer.WriteFloat(_enterVelocity);
        }

        internal void ReadEnteringData(StateBinaryReader reader)
        {
            _distanceBeyondEnd = reader.ReadFloat();
            _enterVelocity = reader.ReadFloat();
            _vehicle.Velocity = _enterVelocity;
            if (_distanceBeyondEnd > 0f)
            {
                UpdateTrainPoints(_vehicle.Length, _distanceBeyondEnd);
                SimpleManager<GoToDepotManager>.Current!.TrainUpdatePosition(_vehicle as Train);
            }
        }

        public GoToDepotVehicleData(Vehicle vehicle, VehicleDepot depot, GoToDepotOverrideTask task)
        {
            _vehicle = vehicle;
            Depot = depot ? depot : throw new ArgumentNullException(nameof(depot));
            Target = depot.Center;
            Name = depot.Name;
            Task = task;
            TargetConnection = FindEndConnection(Depot.SpawnConnection);
            Stops.Add(TargetConnection);
            IsValid = true;
        }
        
        public void Invalidate()
        {
            if (!IsValid)
                return;
            if (ReferenceEquals(Depot, null) || !Depot.IsBuilt)
            {
                IsValid = false;
                Stops.Clear();
            }
        }

        /** true if moving is done */
        public bool MoveTrainInDepot()
        {
            if (_vehicle is not Train train)
                return true;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_enterVelocity == float.MinValue)
                _enterVelocity = Math.Max(train.Velocity, 2f);

            train.Velocity = _enterVelocity;
            float distance = ScaleDistance(train.Velocity) * Time.smoothDeltaTime;
            GoToDepotManager goToDepotManager = SimpleManager<GoToDepotManager>.Current;
            if (train.FrontBound.Position < 1f)
            {
                //normal travel towards end of the track
                float connLength = train.FrontBound.Connection.Length;
                float distToEnd = connLength * (1f - train.FrontBound.Position);
                TrackPosition frontBound = train.FrontBound;
                if (distance > distToEnd)
                {
                    frontBound.Position = 1f;
                    distance -= distToEnd;
                }
                else
                {
                    frontBound.Position += distance / connLength;
                    distance = 0f;
                }
                goToDepotManager!.SetTrainFrontBound(train, frontBound);
            }

            _distanceBeyondEnd += distance;
            float origLength = train.Length;
            if (distance > 0f)
            {
                //travel beyond end of the track
                UpdateTrainPoints(origLength, distance);
            }

            if (_distanceBeyondEnd > 0f)
            {
                goToDepotManager!.SetTrainLength(train, Math.Max(1f, origLength - _distanceBeyondEnd));
            }
            goToDepotManager!.TrainUpdatePosition(train);
            if (_distanceBeyondEnd > 0f)
            {
                goToDepotManager!.SetTrainLength(train, origLength);
            }
            if (train.RearBound.Connection == train.FrontBound.Connection)
            {
                //all train is in the depot, stop executing own moving
                return true;
            }

            return false;
        }

        private void UpdateTrainPoints(float origLength, float distance)
        {
            bool flipped = _vehicle.Flipped;
            if (_trainPoints == null)
                LoadTrainPoints();
            for (int i = _trainPoints.Count - 1; i >= 0; i--)
            {
                TrackUnitPoint point = _trainPoints[i];
                bool remove;
                if (flipped)
                {
                    remove = point.Offset > origLength - _distanceBeyondEnd;
                }
                else
                {
                    point.Offset -= distance;
                    remove = point.Offset < 0f;
                }

                if (remove)
                {
                    point.Clamped = true;
                    _trainPoints.RemoveAt(i);
                }
            }
        }

        private void LoadTrainPoints()
        {
            _trainPoints = SimpleManager<GoToDepotManager>.Current!.GetVehiclePoints(_vehicle);
        }
        
        private TrackConnection FindEndConnection(TrackConnection spawnConnection)
        {
            TrackConnection conn = spawnConnection;
            if (spawnConnection is RoadConnection roadConn)
            {
//                roadConn.PathNode.LastConnection
                return roadConn.OuterConnections[0].InnerConnection.OuterConnections[0].InnerConnection;//.PathNode.LastConnection;
            }
            while (conn.OuterConnectionCount == 1)
                conn = conn.OuterConnections[0].InnerConnection;
            return conn.InnerConnection;
        }
        
        private float ScaleDistance(float distance)
        {
            return distance / 5f;
        }

    }
}