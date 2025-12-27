using UnityEngine;
using System.Collections.Generic;

public interface actionExecuter {
    public void execute(ref gameInfo gameInfo,ref Package pkg, int power = 0, List<int> target = null, int index = 0);
    public bool isNegative(int power);
}
