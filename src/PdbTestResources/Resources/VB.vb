#ExternalSource ("C:\VB.vb", 1)
#Disable Warning BC42099 ' unused constants

Imports System
Imports System.Collections.Generic
Imports <xmlns:file1="http://stuff/fromFile">
Imports <xmlns="http://stuff/fromFile1">
Imports AliasE = N2.D.E

Namespace N1
    Class C
        Iterator Function Foo() As IEnumerable(Of Integer)
            Dim arr(1) As Integer
            arr(0) = 42

            For Each x In arr
                Yield x
                Yield x
            Next

            For Each x In {1, 2, 3}
                Yield x
                Yield x
            Next

        End Function
    End Class
End Namespace

Namespace N2
    Class D
        Class E
            Sub M()
                Const D1 As Decimal = Nothing
                Const D2 As Decimal = 1.23
                Const DT As DateTime = #1-1-2015#
            End Sub
        End Class
    End Class
End Namespace

Class F
    Sub Tuples()
        For Each x In {1, 2, 3}
            Dim a As (x As Integer, String, z As Integer) = Nothing
        Next

        For Each x In {1, 2, 3}
            Dim a As (u As Integer, String) = Nothing
        Next
    End Sub
End Class

#End ExternalSource

Class NoSequencePoints
    Sub M()
        Console.WriteLine()
    End Sub
End Class


