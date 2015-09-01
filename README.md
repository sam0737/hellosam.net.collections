Hellosam.Net.Collections
========================

A set of collections class with the focus of thread-safe and WPF.

NuGet is published at https://www.nuget.org/packages/hellosam.net.collections
The binary is compiled for .Net 3.5 but can also be used for .Net 4.0, 4.5, 4.6 too.

Hellosam.Net.Collections.BinaryTree
-----------------------------------
Represents collection of key/value pairs (dictionary) implemented in binary tree.

Hellosam.Net.Collections.AVLTree
--------------------------------
Represents collection of key/value pairs (dictionary) implemented in AVL tree. Insertion, removal, lookup operations are in O(log n)

Hellosam.Net.Collections.ObservableDictionary
-------------------------------------------------------
Represents collection of key/value pairs (dictionary) implemented in AVL tree. 
This implements INotifyPropertyChanged and INotifyCollectionChanged hence it is observable by WPF items content controls.

Please noted that insertion and removal time complexity is subjected to framework Observer, which usually operates in O(n).

Hellosam.Net.Collections.ObservableDictionaryWithNotification
-------------------------------------------------------
Same as Hellosam.Net.Collections.ObservableDictionary, plus providing "self-replace" Collection changed event when element
property is updated. This is usable only with value type which implements INotifyPropertyChanged.

When bound to a CollectionView, this feature allows Grouping/Sorting to update.

Consider using DeferRequest() to consolidate & ThreadSafeObservableDictionaryWithNotification
-------------------------------------------------------
Thread-safe verision of above.
INotifyPropertyChanged and INotifyCollectionChanged are sent on another thread and hence does not block the writer.


Miscellaneous
=============
Forks and Patches are welcome. Currently the tests file is incompleted. Use at your own risk.
The ThreadSafe version is less battle tested.

Source Code and Publishing
--------------------------
Source code of this extension is available at https://github.com/sam0737/hellosam.net.collections

License And Copyright
---------------------
This project is licensed in Artistic License 2.0

See http://www.perlfoundation.org/artistic_license_2_0

Copyright (c) 2012 Sam Wong
