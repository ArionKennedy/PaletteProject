import { useState } from 'react'
import { Input } from "@/components/ui/input"
import { View } from 'react-native';

export default function SearchBar({ onSubmitSearch }: { onSubmitSearch: Function }) {
    const [searchQuery, setSearchQuery] = useState('');

    function handleSearchSubmit(){
        if(searchQuery){
            onSubmitSearch(searchQuery);
        }
    }

    return (
        <View style={{ width: '100%' }}>
            <Input
                id="searchBar"
                placeholder="Search..."
                className="focus-visible:border-input focus-visible:ring-0"
                keyboardType="default"
                autoCapitalize="none"
                onSubmitEditing={handleSearchSubmit}
                returnKeyType="search"
                submitBehavior="blurAndSubmit"
                onChangeText={setSearchQuery}
            />
        </View>
    );
}