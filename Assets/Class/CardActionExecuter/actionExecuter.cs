using UnityEngine;
using System.Collections.Generic;

public interface actionExecuter {
    public Package execute(ref gameInfo gameInfo, int power = 0, List<int> target = null, int index = 0);
}
