using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.Tracks.Roads;
using VoxelTycoon.Tracks.Tasks;

namespace DepotExtended.GoToDepot
{
    public class GoToDepotVehicleData: IVehicleDestination
    {
        private readonly VehicleDepot _depot;
        public string Name { get; }

        public HashSet<TrackConnection> Stops { get; } = new();
        public TrackConnection TargetConnection { get; }
        public Vector3 Target { get; }
        public bool IsValid { get; private set; }
        public TurnAroundOverrideTask TurningTask { get; set; }
        public GoToDepotOverrideTask Task { get; }

        public GoToDepotVehicleData(VehicleDepot depot, GoToDepotOverrideTask task)
        {
            _depot = depot ? depot : throw new ArgumentNullException(nameof(depot));
            Target = depot.Center;
            Name = depot.Name;
            Task = task;
            TargetConnection = FindEndConnection(_depot.SpawnConnection);
            Stops.Add(TargetConnection);
            IsValid = true;
        }
        
        public void Invalidate()
        {
            if (!IsValid)
                return;
            if (ReferenceEquals(_depot, null) || !_depot.IsBuilt)
            {
                IsValid = false;
                Stops.Clear();
            }
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
    }
}